import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    roles?: string[]
    keepAlive?: boolean
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('../components/TheWelcome.vue'),
      meta: { requiresAuth: true, keepAlive: true }
    },
    {
      path: '/page/:id',
      name: 'page',
      component: () => import('../views/PageView.vue'),
      meta: { keepAlive: true } // Auth will be checked dynamically based on page.IsPublic
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/LoginView.vue')
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('../views/AdminView.vue'),
      meta: { requiresAuth: true, roles: ['Admin'], keepAlive: false }
    },
    {
      path: '/profile',
      name: 'profile',
      component: () => import('../views/UserProfile.vue'),
      meta: { requiresAuth: true, keepAlive: false }
    }
  ]
})

router.beforeEach(async (to, from, next) => {
  const auth = useAuthStore()

  // For page routes, check if page is public before requiring auth
  if (to.name === 'page' && to.params.id) {
    try {
      // Fetch page metadata to check if it's public
      const response = await fetch(`/api/pages/${to.params.id}`, {
        headers: auth.token ? { 'Authorization': `Bearer ${auth.token}` } : {}
      })

      if (response.ok) {
        const page = await response.json()
        // If page is public, allow access regardless of auth
        if (page.isPublic) {
          next()
          return
        }
        // If page is not public, require authentication
        if (!auth.isAuthenticated) {
          next('/login')
          return
        }
      } else if (response.status === 401 || response.status === 403) {
        // Not authenticated or not authorized
        if (!auth.isAuthenticated) {
          next('/login')
          return
        }
        // Authenticated but not authorized - show error in page view
        next()
        return
      } else if (response.status === 404) {
        // Page not found - let the view handle it
        next()
        return
      }
    } catch (error) {
      console.error('Error checking page access:', error)
      // On error, fall through to standard auth check
    }
  }

  // Standard auth check for other routes
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    next('/login')
    return
  }

  if (to.meta.roles) {
    const hasRole = to.meta.roles.some((role: string) => auth.roles.includes(role))
    if (!hasRole) {
      // Could redirect to unauthorized page or show notification
      alert('Unauthorized access')
      next(false)
      return
    }
  }

  next()
})

export default router
