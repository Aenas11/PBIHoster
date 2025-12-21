import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { jwtDecode, type JwtPayload } from 'jwt-decode'
import { api } from '../services/api'

interface CustomJwtPayload extends JwtPayload {
  role?: string | string[]
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[]
  Group?: string | string[]
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string>(localStorage.getItem('token') || '')
  const user = ref<unknown | null>(null)

  const isAuthenticated = computed(() => !!token.value)

  const roles = computed<string[]>(() => {
    if (!token.value) return []
    try {
      const decoded = jwtDecode<CustomJwtPayload>(token.value)
      // ASP.NET Core Identity often puts roles in "role" or "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
      const roleClaim = decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      if (Array.isArray(roleClaim)) return roleClaim
      if (roleClaim) return [roleClaim]
      return []
    } catch {
      return []
    }
  })

  const groups = computed<string[]>(() => {
    if (!token.value) return []
    try {
      const decoded = jwtDecode<CustomJwtPayload>(token.value)
      const groupClaim = decoded.Group
      if (Array.isArray(groupClaim)) return groupClaim
      if (groupClaim) return [groupClaim]
      return []
    } catch {
      return []
    }
  })

  function setToken(newToken: string) {
    token.value = newToken
    localStorage.setItem('token', newToken)
  }

  function logout() {
    token.value = ''
    user.value = null
    localStorage.removeItem('token')
  }

  async function login(username: string, password: string) {
    try {
      const data = await api.post<{ token: string }>('/auth/login',
        { username, password },
        { skipAuth: true,
          skipErrorToast: true
         }
      )
      setToken(data.token)
      return true
    } catch (error) {
      console.error(error)
      throw error
    }
  }

  async function register(username: string, password: string) {
    try {
      await api.post('/auth/register',
        { username, password },
        { skipAuth: true,
          skipErrorToast: true
         }
      )
      return true
    } catch (error) {
      console.error(error)
      throw error
    }
  }

  return { token, user, isAuthenticated, roles, groups, setToken, logout, login, register }
})
