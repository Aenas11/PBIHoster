<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from './stores/auth'
import {Logout20, Switcher20, Tools20 } from '@carbon/icons-vue'
import TheFooter from './components/TheFooter.vue'
import TheSideMenu from './components/TheSideMenu.vue'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const showShell = computed(() => route.name !== 'login')

// Responsive state
const isSideNavExpanded = ref(window.innerWidth > 1056)
const isSideNavFixed = ref(window.innerWidth > 1056)

function onResize() {
  const isDesktop = window.innerWidth > 1056
  isSideNavFixed.value = isDesktop
  
  // Auto-expand on desktop, auto-collapse on mobile
  if (isDesktop) {
    if (!isSideNavExpanded.value) isSideNavExpanded.value = true
  } else {
    if (isSideNavExpanded.value) isSideNavExpanded.value = false
  }
}

onMounted(() => {
  window.addEventListener('resize', onResize)
})

onUnmounted(() => {
  window.removeEventListener('resize', onResize)
})

function handleLogout() {
  auth.logout()
  router.push('/login')
}

function toggleMenu() {
  isSideNavExpanded.value = !isSideNavExpanded.value
}

const isSwitcherExpanded = ref(false)
function toggleSwitcher() {
  isSwitcherExpanded.value = !isSwitcherExpanded.value
}

const contentMargin = computed(() => {
  return isSideNavFixed.value && isSideNavExpanded.value ? 'margin-left: 16rem' : 'margin-left: 3rem'
})
</script>

<template>
  <div class="app-wrapper">
    <div v-if="showShell" class="app-with-shell">
      <cv-header aria-label="ReportTree">
        <cv-header-menu-button 
          aria-label="Header menu" 
          aria-controls="side-nav" 
          @click="toggleMenu"
          :active="isSideNavExpanded"
        />
        <cv-header-name href="#" prefix="">ReportTree</cv-header-name>
        <template v-slot:header-global>
          <cv-header-global-action aria-label="Logout" label="Logout" @click="handleLogout" tip-position="bottom" tip-alignment="end">
            <Logout20 />
          </cv-header-global-action>
          <!-- action that opens toolbox (right menu) with tools to edit current page -->
           <cv-header-global-action aria-label="Tools" label="Tools"
            :active="isSwitcherExpanded"
            @click="toggleSwitcher"
            tip-position="bottom" tip-alignment="end">
            <Switcher20 />
          </cv-header-global-action>
        </template>
        <!-- right menue -->
         <template v-slot:right-panels>
          <cv-header-panel id="switcher-panel" :expanded="isSwitcherExpanded">
            <!-- Placeholder for tools content -->
            <div style="padding: 1rem; width: 200px;">
              <h3>Tools Menu</h3>
              <p>Tools content goes here.</p>
            </div>
          </cv-header-panel>
        </template>
      </cv-header>
      <TheSideMenu 
        :fixed="isSideNavFixed" 
        v-model:expanded="isSideNavExpanded" 
        class="custom-side-nav"
      />
      <cv-content id="main-content" :style="contentMargin">
        <router-view />
      </cv-content>
    </div>
    <div v-else class="login-layout">
      <router-view />
    </div>
    <TheFooter />
  </div>
</template>

<style scoped>
.app-wrapper {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.app-with-shell {
  flex: 1;
  display: flex;
  flex-direction: column;
  position: relative;
  overflow: hidden;
}

:deep(.custom-side-nav) {
  position: absolute !important;
  top: 3rem !important;
  height: calc(100% - 3rem) !important;
  z-index: 900;
}

.login-layout {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

#main-content {
  flex: 1;
  overflow-y: auto;
  transition: margin-left 0.11s cubic-bezier(0.2, 0, 1, 0.9);
  background-color: rebeccapurple;
}
</style>
