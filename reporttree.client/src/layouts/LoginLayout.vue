<script setup lang="ts">
import TheFooter from '../components/TheFooter.vue'
// Simple layout for login page - no authentication required
</script>

<template>
  <div class="login-layout">
    <div class="login-content">
      <router-view />
    </div>
    <TheFooter />
  </div>
</template>

<style scoped>
.login-layout {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.login-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}
</style>
