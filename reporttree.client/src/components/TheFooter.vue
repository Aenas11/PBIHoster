<script setup lang="ts">
import { computed } from 'vue'
import { useStaticSettingsStore } from '@/stores/staticSettings'

const staticSettings = useStaticSettingsStore()
const versionLabel = computed(() => staticSettings.version ? `v${staticSettings.version}` : '')
const appName = computed(() => staticSettings.appName || 'ReportTree')
const footerText = computed(() => {
  if (staticSettings.footerText) {
    return staticSettings.footerText
  }
  const year = new Date().getFullYear()
  return `${appName.value} ${versionLabel.value} © ${year}`.trim()
})
const footerLinkUrl = computed(() => staticSettings.footerLinkUrl || '')
const footerLinkLabel = computed(() => staticSettings.footerLinkLabel || '')
</script>

<template>
  <footer class="app-footer">
    <div class="cds--grid cds--grid--full-width">
      <div class="cds--row">
        <div class="cds--col-lg-16">
          <p class="footer-text">
            {{ footerText }}
            <a v-if="footerLinkUrl" :href="footerLinkUrl" target="_blank" rel="noopener noreferrer" class="footer-link">
              {{ footerLinkLabel || footerLinkUrl }}
            </a>
          </p>
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
  position: relative;
  z-index: 9000;
  /* Ensure footer is always above side nav */
}

.footer-text {
  margin: 0;
  padding: 0;
  font-size: 0.875rem;
  color: #525252;
}

.footer-link {
  margin-left: 0.5rem;
  color: inherit;
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
