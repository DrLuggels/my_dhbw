import { createRouter, createWebHistory } from 'vue-router'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/dashboard',
    },
    {
      path: '/dashboard',
      component: () => import('./views/DashboardView.vue'),
    },
    {
      path: '/documents',
      component: () => import('./views/DocumentsView.vue'),
    },
    {
      path: '/learning',
      component: () => import('./views/LearningView.vue'),
    },
    {
      path: '/knowledge',
      component: () => import('./views/KnowledgeGraphView.vue'),
    },
    {
      path: '/calendar',
      component: () => import('./views/CalendarView.vue'),
    },
    {
      path: '/settings',
      component: () => import('./views/SettingsView.vue'),
    },
  ],
})
