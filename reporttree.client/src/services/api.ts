/**
 * Centralized API client for making HTTP requests
 * Automatically handles authentication headers and common error handling
 */

import { useToastStore } from '../stores/toast'
import { reportApiError } from './monitoring'

interface RequestOptions extends RequestInit {
    skipAuth?: boolean
    skipErrorToast?: boolean
}

class ApiClient {
    private baseUrl = '/api'

    /**
     * Get authentication token from localStorage
     */
    private getAuthHeader(): Record<string, string> {
        const token = localStorage.getItem('token')
        return token ? { 'Authorization': `Bearer ${token}` } : {}
    }

    /**
     * Make a fetch request with automatic auth header injection
     */
    private async request<T>(
        url: string,
        options: RequestOptions = {}
    ): Promise<T> {
        const { skipAuth, skipErrorToast, headers, ...restOptions } = options

        const requestHeaders: HeadersInit = {
            'Content-Type': 'application/json',
            ...headers,
            ...(skipAuth ? {} : this.getAuthHeader())
        }

        try {
            const response = await fetch(`${this.baseUrl}${url}`, {
                ...restOptions,
                headers: requestHeaders
            })

            if (!response.ok) {
                const correlationId = response.headers.get('X-Correlation-ID') ?? undefined
                if (response.status === 401) {
                    // Unauthorized - potentially redirect to login
                    window.dispatchEvent(new CustomEvent('auth:unauthorized'))
                }

                // Try to get error message from response
                let errorMessage = `${response.status} ${response.statusText}`
                let errors: string[] | undefined
                let responseBody: string | undefined
                try {
                    const errorData = await response.json()
                    responseBody = JSON.stringify(errorData)

                    // Handle custom error format with Errors array (password validation, etc.)
                    if (errorData.errors && Array.isArray(errorData.errors)) {
                        errors = errorData.errors
                        if (errors) {
                            errorMessage = errors.join('; ')
                        }
                    }
                    // Handle ASP.NET Core validation errors format
                    else if (errorData.errors && typeof errorData.errors === 'object') {
                        // ASP.NET returns { errors: { field: ["error1", "error2"] } }
                        const allErrors: string[] = []
                        for (const field in errorData.errors) {
                            const fieldErrors = errorData.errors[field]
                            if (Array.isArray(fieldErrors)) {
                                allErrors.push(...fieldErrors)
                            }
                        }
                        if (allErrors.length > 0) {
                            errorMessage = allErrors.join('; ')
                        }
                    }
                    // Handle simple message or title
                    else if (errorData.message) {
                        errorMessage = errorData.message
                    } else if (errorData.title && errorData.title !== 'One or more validation errors occurred.') {
                        errorMessage = errorData.title
                    }
                } catch {
                    // Ignore JSON parse errors
                }

                await reportApiError({
                    message: errorMessage,
                    path: url,
                    method: options.method ?? 'GET',
                    status: response.status,
                    correlationId,
                    responseBody
                })

                if (!skipErrorToast) {
                    const toastStore = useToastStore()
                    toastStore.error('Request Failed', errorMessage)
                }

                throw new Error(errorMessage)
            }

            // Handle no content responses
            if (response.status === 204) {
                return undefined as T
            }

            // Check if response has content
            const contentLength = response.headers.get('content-length')
            const contentType = response.headers.get('content-type')

            // If no content or content-length is 0, return undefined
            if (contentLength === '0' || (!contentType?.includes('application/json') && contentLength === null)) {
                return undefined as T
            }

            // Try to parse JSON, but handle empty responses gracefully
            const text = await response.text()
            if (!text || text.trim() === '') {
                return undefined as T
            }

            return JSON.parse(text)
        } catch (error) {
            // Handle network errors
            if (error instanceof Error && error.message.includes('fetch')) {
                await reportApiError({
                    message: error.message,
                    path: url,
                    method: options.method ?? 'GET'
                })
                if (!skipErrorToast) {
                    const toastStore = useToastStore()
                    toastStore.error('Network Error', 'Unable to connect to the server')
                }
            }
            throw error
        }
    }

    /**
     * GET request
     */
    async get<T>(url: string, options?: RequestOptions): Promise<T> {
        return this.request<T>(url, { ...options, method: 'GET' })
    }

    /**
     * POST request
     */
    async post<T>(url: string, data?: unknown, options?: RequestOptions): Promise<T> {
        return this.request<T>(url, {
            ...options,
            method: 'POST',
            body: data ? JSON.stringify(data) : undefined
        })
    }

    /**
     * PUT request
     */
    async put<T>(url: string, data?: unknown, options?: RequestOptions): Promise<T> {
        return this.request<T>(url, {
            ...options,
            method: 'PUT',
            body: data ? JSON.stringify(data) : undefined
        })
    }

    /**
     * DELETE request
     */
    async delete<T>(url: string, options?: RequestOptions): Promise<T> {
        return this.request<T>(url, { ...options, method: 'DELETE' })
    }
}

export const api = new ApiClient()
