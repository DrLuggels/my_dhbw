<script setup lang="ts">
import { calendarApi } from '@/api/calendar'
import LoadingState from '@/components/common/LoadingState.vue'
import { computed, onMounted, ref } from 'vue'

interface CalEvent {
  id: number
  title: string
  description: string | null
  start_time: string
  end_time: string | null
  event_type: string
  source: string
  location: string | null
}

const events = ref<CalEvent[]>([])
const loading = ref(true)
const syncing = ref(false)
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
  syncing.value = true
  try {
    await calendarApi.syncRapla()
    const { data } = await calendarApi.events()
    events.value = (data.data ?? []) as CalEvent[]
  } finally {
    syncing.value = false
  }
}

const groupedEvents = computed(() => {
  const groups: Record<string, CalEvent[]> = {}
  for (const ev of events.value) {
    const key = new Date(ev.start_time).toLocaleDateString('de-DE', {
      weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric',
    })
    ;(groups[key] ??= []).push(ev)
  }
  return groups
})

function formatTime(dateStr: string): string {
  return new Date(dateStr).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' })
}

function timeRange(ev: CalEvent): string {
  const start = formatTime(ev.start_time)
  const end = ev.end_time ? formatTime(ev.end_time) : ''
  return end ? `${start} – ${end}` : start
}
</script>

<template>
  <div>
    <v-toolbar flat color="transparent">
      <v-toolbar-title>Stundenplan</v-toolbar-title>
      <v-spacer />
      <v-btn
        color="primary"
        variant="outlined"
        prepend-icon="mdi-sync"
        :loading="syncing"
        @click="syncRapla"
      >
        Rapla Sync
      </v-btn>
    </v-toolbar>

    <v-container fluid class="pa-6">
      <LoadingState :loading="loading" :error="error">
        <v-empty-state
          v-if="!events.length"
          icon="mdi-calendar-blank"
          title="Keine Events"
          text="Synchronisiere Rapla um deinen Stundenplan zu laden"
        />

        <div v-else>
          <div v-for="(dayEvents, day) in groupedEvents" :key="day" class="mb-6">
            <div class="text-subtitle-1 font-weight-bold mb-2">{{ day }}</div>
            <v-card
              v-for="event in dayEvents"
              :key="event.id"
              elevation="1"
              rounded="lg"
              class="mb-2"
            >
              <v-card-text class="d-flex align-center py-3">
                <v-icon
                  :color="event.event_type === 'lecture' ? 'primary' : 'accent'"
                  class="mr-3"
                  size="24"
                >
                  {{ event.event_type === 'lecture' ? 'mdi-school' : 'mdi-calendar' }}
                </v-icon>
                <div class="flex-grow-1">
                  <div class="font-weight-medium">{{ event.title }}</div>
                  <div class="text-body-2 text-medium-emphasis">
                    {{ timeRange(event) }}
                    <span v-if="event.location" class="ml-2">
                      <v-icon size="14" class="mr-1">mdi-map-marker</v-icon>{{ event.location }}
                    </span>
                  </div>
                  <div v-if="event.description" class="text-body-2 text-medium-emphasis">
                    <v-icon size="14" class="mr-1">mdi-account</v-icon>{{ event.description }}
                  </div>
                </div>
                <v-chip size="small" variant="tonal" :color="event.source === 'rapla' ? 'primary' : 'accent'">
                  {{ event.source }}
                </v-chip>
              </v-card-text>
            </v-card>
          </div>
        </div>
      </LoadingState>
    </v-container>
  </div>
</template>
