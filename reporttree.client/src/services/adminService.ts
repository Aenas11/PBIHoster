import { useAuthStore } from '../stores/auth'
import type { AppUser, Group, UpsertUserRequest } from '../types/admin'

async function fetchWithAuth(url: string, options: RequestInit = {}) {
    const auth = useAuthStore()
    const headers = new Headers(options.headers)

    if (auth.token) {
        headers.set('Authorization', `Bearer ${auth.token}`)
    }

    // Default to JSON content type if body is present and not FormData
    if (options.body && typeof options.body === 'string' && !headers.has('Content-Type')) {
        headers.set('Content-Type', 'application/json')
    }

    const response = await fetch(url, { ...options, headers })

    if (!response.ok) {
        // You might want to handle 401/403 specifically here (e.g. logout)
        throw new Error(`API Error: ${response.status} ${response.statusText}`)
    }

    return response
}

export const adminService = {
    async searchUsers(term: string = ''): Promise<AppUser[]> {
        const q = term ? `?term=${encodeURIComponent(term)}` : ''
        const res = await fetchWithAuth(`/api/admin/users${q}`)
        return res.json()
    },

    async upsertUser(user: UpsertUserRequest): Promise<void> {
        await fetchWithAuth('/api/admin/users', {
            method: 'POST',
            body: JSON.stringify(user)
        })
    },

    async deleteUser(username: string): Promise<void> {
        await fetchWithAuth(`/api/admin/users/${encodeURIComponent(username)}`, {
            method: 'DELETE'
        })
    },

    async searchGroups(term: string = ''): Promise<Group[]> {
        const q = term ? `?term=${encodeURIComponent(term)}` : ''
        const res = await fetchWithAuth(`/api/admin/groups${q}`)
        return res.json()
    },

    async createGroup(group: Group): Promise<Group> {
        const res = await fetchWithAuth('/api/admin/groups', {
            method: 'POST',
            body: JSON.stringify(group)
        })
        return res.json()
    },

    async updateGroup(group: Group): Promise<void> {
        if (!group.id) throw new Error('Group ID required for update')
        await fetchWithAuth(`/api/admin/groups/${group.id}`, {
            method: 'PUT',
            body: JSON.stringify(group)
        })
    },

    async deleteGroup(id: number): Promise<void> {
        await fetchWithAuth(`/api/admin/groups/${id}`, {
            method: 'DELETE'
        })
    }
}
