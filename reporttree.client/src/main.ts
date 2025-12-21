import './assets/main.css'
import './assets/styles.scss'
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { registerDashboardComponents } from './config/components'

// Register all dashboard components before creating the app
registerDashboardComponents()

const app = createApp(App)

app.use(createPinia())
app.use(router)


app.mount('#app')