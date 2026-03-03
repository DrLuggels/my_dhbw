<script setup lang="ts">
import { calendarApi } from '@/api/calendar'
import LoadingState from '@/components/common/LoadingState.vue'
import { onMounted, ref } from 'vue'

interface CalEvent {
  id: number
  title: string
  start_time: string
  end_time: string | null
  event_type: string
  source: string
  location: string | null
}

const events = ref<CalEvent[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    const { data } = await calendarApi.events()
    events.value = (data.data ?? []) as CalEvent[]
  } catch {
    error.value = 'Kalender konnte nicht geladen werden'
  } finally {
    loading.value = false
  }
})

async function syncRapla() {
  await calendarApi.syncRapla()
  const { data } = await calendarApi.events()
  events.value = (data.data ?? []) as CalEvent[]
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('de-DE', {
    weekday: 'short', day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit',
  })
}
</script>

<template>
  <div>
    <v-toolbar flat color="transparent">
      <v-toolbar-title>Kalender</v-toolbar-title>
      <v-spacer />
      <v-btn color="primary" variant="outlined" prepend-icon="mdi-sync" @click="syncRapla">
        Rapla Sync
      </v-btn>
    </v-toolbar>

    <v-container fluid class="pa-6">
      <LoadingState :loading="loading" :error="error">
        <v-empty-state
          v-if="!events.length"
          icon="mdi-calendar-blank"
          title="Keine Events"
          text="Synchronisiere Rapla oder erstelle manuelle Events"
        />

        <v-list v-else>
          <v-list-item
            v-for="event in events"
            :key="event.id"
            :title="event.title"
            :subtitle="formatDate(event.start_time)"
          >
            <template #prepend>
              <v-icon :color="event.source === 'rapla' ? 'primary' : 'accent'">
                {{ event.event_type === 'lecture' ? 'mdi-school' : 'mdi-calendar' }}
              </v-icon>
            </template>
            <template #append>
              <v-chip size="small">{{ event.source }}</v-chip>
            </template>
          </v-list-item>
        </v-list>
      </LoadingState>
    </v-container>
  </div>
</template>
