import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { jwtDecode } from 'jwt-decode'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token') || '')
  const user = ref(null)

  const isAuthenticated = computed(() => !!token.value)
  
  const roles = computed(() => {
    if (!token.value) return []
    try {
      const decoded = jwtDecode(token.value)
      // ASP.NET Core Identity often puts roles in "role" or "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
      const roleClaim = decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      if (Array.isArray(roleClaim)) return roleClaim
      if (roleClaim) return [roleClaim]
      return []
    } catch (e) {
      return []
    }
  })

  function setToken(newToken) {
    token.value = newToken
    localStorage.setItem('token', newToken)
  }

  function logout() {
    token.value = ''
    user.value = null
    localStorage.removeItem('token')
  }

  async function login(username, password) {
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
      return false
    }
  }

  return { token, user, isAuthenticated, roles, login, logout }
})
