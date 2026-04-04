import { api } from './api'

export interface ExternalAuthProviderSummary {
    id: string
    displayName: string
    scheme: string
}

export interface ExternalCallbackResult {
    token?: string
    error?: string
}

export const authService = {
    async getExternalProviders(): Promise<ExternalAuthProviderSummary[]> {
        return api.get<ExternalAuthProviderSummary[]>('/auth/external/providers', {
            skipAuth: true,
            skipErrorToast: true
        })
    },

    startExternalLogin(providerId: string, returnUrl: string): void {
        const target = `/api/auth/external/challenge/${encodeURIComponent(providerId)}?returnUrl=${encodeURIComponent(returnUrl)}`
        window.location.assign(target)
    },

    parseExternalCallbackHash(hash: string): ExternalCallbackResult {
        if (!hash) {
            return { error: 'Missing callback payload' }
        }

        const normalized = hash.startsWith('#') ? hash.slice(1) : hash
        const params = new URLSearchParams(normalized)
        const token = params.get('token')
        const authError = params.get('authError')

        if (token) {
            return { token }
        }

        if (authError) {
            return { error: decodeURIComponent(authError) }
        }

        return { error: 'External login did not return a token' }
    }
}
