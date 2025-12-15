import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import TheWelcome from '../components/TheWelcome.vue'
import Login from '../views/Login.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: TheWelcome,
      meta: { requiresAuth: true }
    },
    {
      path: '/login',
      name: 'login',
      component: Login
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('../components/HelloWorld.vue'), // Placeholder for Admin
      meta: { requiresAuth: true, roles: ['Admin'] }
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
    const hasRole = to.meta.roles.some(role => auth.roles.includes(role))
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
