import './assets/main.css'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import 'carbon-components/css/carbon-components.css'
// import 'grid-layout-plus/dist/style.css'
import CarbonVue3 from '@carbon/vue'
import { registerDashboardComponents } from './config/components'

// Register all dashboard components before creating the app
registerDashboardComponents()

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(CarbonVue3)

app.mount('#app')