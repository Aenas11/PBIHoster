import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import TheWelcome from '../components/TheWelcome.vue'
import Login from '../views/LoginView.vue'

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
      component: TheWelcome,
      meta: { requiresAuth: true, keepAlive: true }
    },
    {
      path: '/page/:id',
      name: 'page',
      component: () => import('../views/PageView.vue'),
      meta: { requiresAuth: true, keepAlive: true }
    },
    {
      path: '/login',
      name: 'login',
      component: Login
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('../views/AdminView.vue'),
      meta: { requiresAuth: true, roles: ['Admin'], keepAlive: false }
    }
  ]
})

router.beforeEach((to, from, next) => {
  const auth = useAuthStore()

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
