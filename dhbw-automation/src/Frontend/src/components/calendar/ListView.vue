<template>
  <v-data-table
    :headers="headers"
    :items="events"
    :loading="loading"
    :items-per-page="25"
    class="elevation-1"
    hover
    @click:row="(_event: any, { item }: any) => emit('event-click', item)"
  >
    <template v-slot:item.startTime="{ item }">
      {{ formatDateTime(item.startTime) }}
    </template>

    <template v-slot:item.endTime="{ item }">
      {{ formatDateTime(item.endTime) }}
    </template>

    <template v-slot:item.source="{ item }">
      <v-chip :color="getSourceColor(item.source)" size="small">
        {{ item.source }}
      </v-chip>
    </template>

    <template v-slot:item.eventType="{ item }">
      <v-chip v-if="item.eventType" color="info" size="small">
        {{ item.eventType }}
      </v-chip>
    </template>
  </v-data-table>
</template>

<script setup lang="ts">
import type { CalendarEvent } from '@/types/calendar'

interface Props {
  events: CalendarEvent[]
  loading: boolean
}

defineProps<Props>()
const emit = defineEmits<{
  'event-click': [event: CalendarEvent]
}>()

const headers = [
  { title: 'Titel', key: 'title', sortable: true },
  { title: 'Fach', key: 'subject', sortable: true },
  { title: 'Kurstyp', key: 'eventType', sortable: true },
  { title: 'Start', key: 'startTime', sortable: true },
  { title: 'Ende', key: 'endTime', sortable: true },
  { title: 'Ort', key: 'location', sortable: true },
  { title: 'Quelle', key: 'source', sortable: true }
]

const formatDateTime = (dateString: string) => {
  const date = new Date(dateString)
  return date.toLocaleString('de-DE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const getSourceColor = (source: string) => {
  switch (source) {
    case 'rapla': return 'primary'
    case 'moodle': return 'secondary'
    case 'manual': return 'info'
    case 'learning': return 'success'
    case 'ai_generated': return 'success'
    default: return 'grey'
  }
}
</script>

<style scoped>
:deep(.v-data-table tbody tr) {
  cursor: pointer;
}

:deep(.v-data-table tbody tr:hover) {
  background-color: rgba(0, 0, 0, 0.04);
}
</style>
