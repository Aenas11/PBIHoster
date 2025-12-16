import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { jwtDecode, type JwtPayload } from 'jwt-decode'

interface CustomJwtPayload extends JwtPayload {
  role?: string | string[]
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[]
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
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password })
      })

      if (!response.ok) throw new Error('Login failed')

      const data = await response.json()
      setToken(data.token)
      return true
    } catch (error) {
      console.error(error)
      throw error
    }
  }

  return { token, user, isAuthenticated, roles, setToken, logout, login }
})
