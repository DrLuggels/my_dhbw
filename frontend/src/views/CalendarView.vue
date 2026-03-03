<script setup lang="ts">
import { calendarApi } from '@/api/calendar'
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

const allEvents = ref<CalEvent[]>([])
const loading = ref(true)
const syncing = ref(false)
const currentMonday = ref(getMonday(new Date()))

function getMonday(d: Date): Date {
  const date = new Date(d)
  const day = date.getDay()
  const diff = date.getDate() - day + (day === 0 ? -6 : 1)
  date.setDate(diff)
  date.setHours(0, 0, 0, 0)
  return date
}

function addDays(d: Date, n: number): Date {
  const r = new Date(d)
  r.setDate(r.getDate() + n)
  return r
}

const weekDays = computed(() =>
  Array.from({ length: 6 }, (_, i) => {
    const d = addDays(currentMonday.value, i)
    return {
      date: d,
      label: d.toLocaleDateString('de-DE', { weekday: 'short' }),
      dateLabel: d.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit' }),
      iso: d.toISOString().slice(0, 10),
    }
  })
)

const weekLabel = computed(() => {
  const mon = currentMonday.value
  const fri = addDays(mon, 4)
  const opts: Intl.DateTimeFormatOptions = { day: '2-digit', month: '2-digit', year: 'numeric' }
  return `${mon.toLocaleDateString('de-DE', opts)} – ${fri.toLocaleDateString('de-DE', opts)}`
})

const isCurrentWeek = computed(() => {
  const today = getMonday(new Date())
  return currentMonday.value.getTime() === today.getTime()
})

function prevWeek() { currentMonday.value = addDays(currentMonday.value, -7) }
function nextWeek() { currentMonday.value = addDays(currentMonday.value, 7) }
function goToday() { currentMonday.value = getMonday(new Date()) }

const START_HOUR = 8
const END_HOUR = 19
const HOUR_HEIGHT = 60
const hours = Array.from({ length: END_HOUR - START_HOUR }, (_, i) => START_HOUR + i)

const weekEvents = computed(() => {
  const map: Record<string, CalEvent[]> = {}
  for (const day of weekDays.value) map[day.iso] = []
  for (const ev of allEvents.value) {
    const iso = new Date(ev.start_time).toLocaleDateString('en-CA')
    if (map[iso]) map[iso].push(ev)
  }
  return map
})

function eventStyle(ev: CalEvent) {
  const start = new Date(ev.start_time)
  const end = ev.end_time ? new Date(ev.end_time) : new Date(start.getTime() + 3600000)
  const startMin = (start.getHours() - START_HOUR) * 60 + start.getMinutes()
  const duration = (end.getTime() - start.getTime()) / 60000
  return {
    top: `${startMin}px`,
    height: `${Math.max(duration, 30)}px`,
  }
}

const PALETTE = [
  '#1565C0', '#2E7D32', '#6A1B9A', '#E65100', '#00838F',
  '#AD1457', '#283593', '#4E342E', '#00695C', '#BF360C',
  '#1B5E20', '#4A148C', '#0D47A1', '#880E4F', '#33691E',
]
const courseColorMap = new Map<string, string>()
let colorIndex = 0

function isExam(ev: CalEvent): boolean {
  const t = ev.title.toLowerCase()
  return t.includes('klausur') || t.includes('kurztest') || t.includes('prüfung') || t.includes('exam')
}

function getCourseKey(title: string): string {
  // Extract course code like "W4DSKI_107.2" or use cleaned title
  const match = title.match(/\(([^)]+)\)/)
  if (match) return match[1].trim()
  // For events without code, normalize the title
  return title.replace(/\s+/g, ' ').trim().toLowerCase()
}

function eventColor(ev: CalEvent): string {
  if (isExam(ev)) return '#C62828'
  const t = ev.title.toLowerCase()
  if (t.includes('tutorium')) return '#546E7A'
  if (t.includes('selbststudium') || t.includes('feiertag') || t.includes('rosenmontag')
    || t.includes('karfreitag') || t.includes('ostermontag') || t.includes('pfingst')
    || t.includes('fronleichnam') || t.includes('himmelfahrt') || t.includes('tag der arbeit')
    || t.includes('ostersamstag')) return '#78909C'

  const key = getCourseKey(ev.title)
  if (!courseColorMap.has(key)) {
    courseColorMap.set(key, PALETTE[colorIndex % PALETTE.length])
    colorIndex++
  }
  return courseColorMap.get(key)!
}

function shortTime(dateStr: string): string {
  return new Date(dateStr).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' })
}

function shortRoom(loc: string | null): string {
  if (!loc) return ''
  // Truncate at first parenthesis (removes repeated date info)
  const idx = loc.indexOf('(')
  return idx > 0 ? loc.slice(0, idx).trim() : loc
}

onMounted(async () => {
  try {
    const { data } = await calendarApi.events()
    allEvents.value = (data.data ?? []) as CalEvent[]
  } finally {
    loading.value = false
  }
})

async function syncRapla() {
  syncing.value = true
  try {
    await calendarApi.syncRapla()
    const { data } = await calendarApi.events()
    allEvents.value = (data.data ?? []) as CalEvent[]
  } finally {
    syncing.value = false
  }
}
</script>

<template>
  <div class="timetable-page">
    <!-- Header -->
    <div class="timetable-header">
      <div class="d-flex align-center ga-2">
        <v-btn icon="mdi-chevron-left" variant="text" size="small" @click="prevWeek" />
        <v-btn
          v-if="!isCurrentWeek"
          variant="tonal"
          size="small"
          color="primary"
          @click="goToday"
        >
          Heute
        </v-btn>
        <v-btn icon="mdi-chevron-right" variant="text" size="small" @click="nextWeek" />
        <span class="text-subtitle-1 font-weight-bold ml-2">{{ weekLabel }}</span>
      </div>
      <v-btn
        color="primary"
        variant="outlined"
        size="small"
        prepend-icon="mdi-sync"
        :loading="syncing"
        @click="syncRapla"
      >
        Rapla Sync
      </v-btn>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="d-flex justify-center pa-12">
      <v-progress-circular indeterminate color="primary" />
    </div>

    <!-- Timetable Grid -->
    <div v-else class="timetable-wrapper">
      <div class="timetable-grid">
        <!-- Day headers -->
        <div class="time-gutter header-cell" />
        <div
          v-for="day in weekDays"
          :key="day.iso"
          class="day-header"
          :class="{ 'today': day.iso === new Date().toLocaleDateString('en-CA') }"
        >
          <div class="day-name">{{ day.label }}</div>
          <div class="day-date">{{ day.dateLabel }}</div>
        </div>

        <!-- Time rows + day columns -->
        <div class="time-gutter">
          <div
            v-for="hour in hours"
            :key="hour"
            class="time-label"
            :style="{ height: HOUR_HEIGHT + 'px' }"
          >
            {{ hour }}:00
          </div>
        </div>

        <div
          v-for="day in weekDays"
          :key="'col-' + day.iso"
          class="day-column"
          :class="{ 'today-col': day.iso === new Date().toLocaleDateString('en-CA') }"
          :style="{ height: (END_HOUR - START_HOUR) * HOUR_HEIGHT + 'px' }"
        >
          <!-- Hour lines -->
          <div
            v-for="hour in hours"
            :key="'line-' + hour"
            class="hour-line"
            :style="{ top: (hour - START_HOUR) * HOUR_HEIGHT + 'px' }"
          />

          <!-- Events -->
          <div
            v-for="ev in weekEvents[day.iso]"
            :key="ev.id"
            class="event-block"
            :class="{ 'exam-block': isExam(ev) }"
            :style="{ ...eventStyle(ev), backgroundColor: eventColor(ev) }"
          >
            <div class="event-title">
              <span v-if="isExam(ev)" class="exam-icon">&#9888; </span>{{ ev.title }}
            </div>
            <div class="event-meta">
              {{ shortTime(ev.start_time) }}<template v-if="ev.end_time"> – {{ shortTime(ev.end_time) }}</template>
            </div>
            <div v-if="ev.location" class="event-meta">{{ shortRoom(ev.location) }}</div>
            <div v-if="ev.description" class="event-meta">{{ ev.description }}</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.timetable-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}

.timetable-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 1px solid #e0e0e0;
  background: #fff;
  flex-shrink: 0;
}

.timetable-wrapper {
  flex: 1;
  overflow: auto;
}

.timetable-grid {
  display: grid;
  grid-template-columns: 56px repeat(6, 1fr);
  min-width: 700px;
}

/* Headers */
.header-cell {
  border-bottom: 2px solid #e0e0e0;
  background: #fafafa;
}

.day-header {
  text-align: center;
  padding: 8px 4px;
  border-bottom: 2px solid #e0e0e0;
  border-left: 1px solid #e0e0e0;
  background: #fafafa;
}

.day-header.today {
  background: #E3F2FD;
}

.day-name {
  font-weight: 600;
  font-size: 13px;
  text-transform: uppercase;
  color: #424242;
}

.day-date {
  font-size: 12px;
  color: #757575;
}

/* Time gutter */
.time-gutter {
  background: #fafafa;
  border-right: 1px solid #e0e0e0;
}

.time-label {
  display: flex;
  align-items: flex-start;
  justify-content: flex-end;
  padding: 0 8px;
  font-size: 11px;
  color: #9e9e9e;
  transform: translateY(-7px);
}

/* Day columns */
.day-column {
  position: relative;
  border-left: 1px solid #e0e0e0;
}

.today-col {
  background: #F5F9FF;
}

.hour-line {
  position: absolute;
  left: 0;
  right: 0;
  height: 1px;
  background: #eeeeee;
}

/* Event blocks */
.event-block {
  position: absolute;
  left: 2px;
  right: 2px;
  border-radius: 4px;
  padding: 3px 6px;
  overflow: hidden;
  color: #fff;
  font-size: 11px;
  line-height: 1.3;
  cursor: default;
  z-index: 1;
  box-shadow: 0 1px 2px rgba(0,0,0,0.15);
  border-left: 3px solid rgba(0,0,0,0.2);
}

.event-title {
  font-weight: 600;
  font-size: 11px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.event-meta {
  font-size: 10px;
  opacity: 0.9;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* Exam styling */
.exam-block {
  border-left: 3px solid #FFD600 !important;
  box-shadow: 0 1px 4px rgba(198, 40, 40, 0.4);
  animation: exam-pulse 2s ease-in-out infinite;
}

.exam-icon {
  font-size: 12px;
}

@keyframes exam-pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.85; }
}
</style>
