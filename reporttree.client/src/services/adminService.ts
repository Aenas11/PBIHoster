import { api } from './api'
import type { AppUser, Group, UpsertUserRequest } from '../types/admin'

export const adminService = {
    async searchUsers(term: string = ''): Promise<AppUser[]> {
        const q = term ? `?term=${encodeURIComponent(term)}` : ''
        return api.get<AppUser[]>(`/admin/users${q}`)
    },

    async upsertUser(user: UpsertUserRequest): Promise<void> {
        await api.post('/admin/users', user)
    },

    async deleteUser(username: string): Promise<void> {
        await api.delete(`/admin/users/${encodeURIComponent(username)}`)
    },

    async searchGroups(term: string = ''): Promise<Group[]> {
        const q = term ? `?term=${encodeURIComponent(term)}` : ''
        return api.get<Group[]>(`/admin/groups${q}`)
    },

    async createGroup(group: Group): Promise<Group> {
        return api.post<Group>('/admin/groups', group)
    },

    async updateGroup(group: Group): Promise<void> {
        if (!group.id) throw new Error('Group ID required for update')
        await api.put(`/admin/groups/${group.id}`, group)
    },

    async deleteGroup(id: number): Promise<void> {
        await api.delete(`/admin/groups/${id}`)
    },

    async addGroupMember(groupId: number, username: string): Promise<void> {
        await api.post(`/admin/groups/${groupId}/members`, { username })
    },

    async removeGroupMember(groupId: number, username: string): Promise<void> {
        await api.delete(`/admin/groups/${groupId}/members/${encodeURIComponent(username)}`)
    }
}
