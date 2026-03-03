import '@mdi/font/css/materialdesignicons.css'
import { createPinia } from 'pinia'
import { createApp } from 'vue'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import 'vuetify/styles'

import App from './App.vue'
import { router } from './router'

const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: 'dhbw',
    themes: {
      dhbw: {
        dark: false,
        colors: {
          primary: '#1565C0',
          secondary: '#37474F',
          accent: '#00897B',
          background: '#FAFAFA',
          surface: '#FFFFFF',
          error: '#D32F2F',
          warning: '#F57C00',
          info: '#1976D2',
          success: '#388E3C',
        },
      },
    },
  },
})

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(vuetify)
app.mount('#app')
