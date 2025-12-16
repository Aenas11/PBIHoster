<script setup lang="ts">
</script>

<template>
  <footer class="app-footer">
    <div class="bx--grid bx--grid--full-width">
      <div class="bx--row">
        <div class="bx--col-lg-16">
          <p class="footer-text">ReportTree &copy; {{ new Date().getFullYear() }}</p>
        </div>
      </div>
    </div>
  </footer>
</template>

<style scoped>
.app-footer {
  width: 100%;
  padding: 1rem;
  border-top: 1px solid #e0e0e0;
  background-color: #f4f4f4;
  margin-top: auto;
}

.footer-text {
  margin: 0;
  padding: 0;
  font-size: 0.875rem;
  color: #525252;
}

@media (prefers-color-scheme: dark) {
  .app-footer {
    border-top-color: #393939;
    background-color: #262626;
  }
  
  .footer-text {
    color: #c6c6c6;
  }
}
</style>
