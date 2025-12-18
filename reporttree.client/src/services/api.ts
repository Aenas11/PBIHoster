/**
 * Centralized API client for making HTTP requests
 * Automatically handles authentication headers and common error handling
 */

interface RequestOptions extends RequestInit {
    skipAuth?: boolean
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
        const { skipAuth, headers, ...restOptions } = options

        const requestHeaders: HeadersInit = {
            'Content-Type': 'application/json',
            ...headers,
            ...(skipAuth ? {} : this.getAuthHeader())
        }

        const response = await fetch(`${this.baseUrl}${url}`, {
            ...restOptions,
            headers: requestHeaders
        })

        if (!response.ok) {
            if (response.status === 401) {
                // Unauthorized - potentially redirect to login
                window.dispatchEvent(new CustomEvent('auth:unauthorized'))
            }
            throw new Error(`API Error: ${response.status} ${response.statusText}`)
        }

        // Handle no content responses
        if (response.status === 204) {
            return undefined as T
        }

        return response.json()
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
