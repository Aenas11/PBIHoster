<script setup>
import { computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from './stores/auth'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const showShell = computed(() => route.name !== 'login')
const isAdmin = computed(() => auth.roles.includes('Admin'))

function handleLogout() {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <div v-if="showShell">
    <cv-header aria-label="ReportTree">
      <cv-header-name href="#">ReportTree</cv-header-name>
      <template v-slot:header-global>
        <cv-header-global-action aria-label="Logout" @click="handleLogout">
          Logout
        </cv-header-global-action>
      </template>
    </cv-header>
    <div style="display:flex; min-height: calc(100vh - 3rem)">
      <cv-side-nav aria-label="Side navigation" fixed>
        <cv-side-nav-items>
          <cv-side-nav-item href="#" @click.prevent="router.push('/')" :active="route.path === '/'">Dashboard</cv-side-nav-item>
          <cv-side-nav-item href="#">Reports</cv-side-nav-item>
          <cv-side-nav-item 
            v-if="isAdmin"
            href="#" 
            @click.prevent="router.push('/admin')"
            :active="route.path === '/admin'"
          >
            Admin
          </cv-side-nav-item>
        </cv-side-nav-items>
      </cv-side-nav>
      <main style="padding:1rem; width:100%">
        <router-view />
      </main>
    </div>
  </div>
  <div v-else>
    <router-view />
  </div>
</template>

<style scoped>
</style>
