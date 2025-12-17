<script setup lang="ts">
// Simple layout for login page - no authentication required
</script>

<template>
  <div class="login-layout">
    <router-view />
  </div>
</template>

<style scoped>
.login-layout {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
}
</style>
