<template>
  <div>
    <div class="mb-4 d-flex justify-space-between align-center">
      <v-btn icon @click="emit('previous-week')">
        <v-icon>mdi-chevron-left</v-icon>
      </v-btn>
      <h3>{{ weekTitle }}</h3>
      <v-btn icon @click="emit('next-week')">
        <v-icon>mdi-chevron-right</v-icon>
      </v-btn>
    </div>

    <div class="week-view">
      <div class="week-grid">
        <div class="time-column">
          <div class="time-header"></div>
          <div v-for="hour in hours" :key="hour" class="time-slot">
            {{ hour }}:00
          </div>
        </div>

        <div v-for="day in weekDays" :key="day.date" class="day-column">
          <div class="day-header" :class="{ 'today': isToday(day.date) }">
            <div class="day-name">{{ day.name }}</div>
            <div class="day-date">{{ formatDate(day.date) }}</div>
          </div>
          <div class="day-events">
            <div v-for="hour in hours" :key="hour" class="hour-slot"></div>
            <div
              v-for="event in getDayEvents(day.date)"
              :key="event.id"
              class="event-card"
              :style="getEventStyle(event)"
              :class="'event-' + event.source"
              @click="emit('event-click', event)"
            >
              <div class="event-time">
                {{ formatTime(event.startTime) }} - {{ formatTime(event.endTime) }}
              </div>
              <div class="event-title">{{ event.title }}</div>
              <div v-if="event.location" class="event-location">
                <v-icon size="x-small">mdi-map-marker</v-icon>
                {{ event.location }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { CalendarEvent } from '@/types/calendar'

interface Props {
  events: CalendarEvent[]
  currentWeekStart: Date
  weekTitle: string
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'previous-week': []
  'next-week': []
  'event-click': [event: CalendarEvent]
}>()

const hours = Array.from({ length: 15 }, (_, i) => i + 7)

// Helper to format date in local timezone (YYYY-MM-DD)
function toLocalDateString(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

const weekDays = computed(() => {
  const days = []
  const dayNames = ['Montag', 'Dienstag', 'Mittwoch', 'Donnerstag', 'Freitag', 'Samstag', 'Sonntag']

  for (let i = 0; i < 7; i++) {
    const date = new Date(props.currentWeekStart)
    date.setDate(date.getDate() + i)
    days.push({
      date: toLocalDateString(date),
      name: dayNames[i]
    })
  }

  return days
})

function isToday(dateStr: string): boolean {
  const today = toLocalDateString(new Date())
  return dateStr === today
}

function getDayEvents(dateStr: string): CalendarEvent[] {
  return props.events.filter(event => {
    const eventDate = toLocalDateString(new Date(event.startTime))
    return eventDate === dateStr
  })
}

function eventsOverlap(event1: CalendarEvent, event2: CalendarEvent): boolean {
  const start1 = new Date(event1.startTime).getTime()
  const end1 = new Date(event1.endTime).getTime()
  const start2 = new Date(event2.startTime).getTime()
  const end2 = new Date(event2.endTime).getTime()
  return start1 < end2 && start2 < end1
}

function getOverlappingEvents(event: CalendarEvent, dayEvents: CalendarEvent[]): { index: number; total: number } {
  const overlapping = dayEvents.filter(e => eventsOverlap(event, e))
  overlapping.sort((a, b) => {
    const startDiff = new Date(a.startTime).getTime() - new Date(b.startTime).getTime()
    if (startDiff !== 0) return startDiff
    return a.id - b.id
  })
  const index = overlapping.findIndex(e => e.id === event.id)
  return { index, total: overlapping.length }
}

function getEventStyle(event: CalendarEvent) {
  const start = new Date(event.startTime)
  const end = new Date(event.endTime)

  const startHour = start.getHours()
  const startMinute = start.getMinutes()
  const endHour = end.getHours()
  const endMinute = end.getMinutes()

  // Calculate position in pixels (60px per hour)
  const startMinutes = startHour * 60 + startMinute
  const endMinutes = endHour * 60 + endMinute
  const gridStartMinutes = 7 * 60  // 7:00
  const gridEndMinutes = 22 * 60   // 22:00
  const maxHeight = 15 * 60        // 900px (15 hours * 60px)

  // Clamp to visible area (like mobile app does)
  const clampedStartMinutes = Math.max(startMinutes, gridStartMinutes)
  const clampedEndMinutes = Math.min(endMinutes, gridEndMinutes)

  const topOffset = ((clampedStartMinutes - gridStartMinutes) / 60) * 60
  const duration = ((clampedEndMinutes - clampedStartMinutes) / 60) * 60

  // Clamp final values
  const clampedTop = Math.max(0, Math.min(topOffset, maxHeight))
  const clampedHeight = Math.max(30, Math.min(duration, maxHeight - clampedTop))

  const dateStr = toLocalDateString(start)
  const dayEvents = getDayEvents(dateStr)
  const { index, total } = getOverlappingEvents(event, dayEvents)

  const widthPercent = total > 1 ? (100 / total) : 100
  const leftPercent = total > 1 ? (index * widthPercent) : 0

  return {
    top: `${clampedTop + 40}px`,
    height: `${clampedHeight}px`,
    left: total > 1 ? `calc(${leftPercent}% + 2px)` : '4px',
    width: total > 1 ? `calc(${widthPercent}% - 4px)` : 'calc(100% - 8px)'
  }
}

const formatTime = (dateString: string) => {
  const date = new Date(dateString)
  return date.toLocaleTimeString('de-DE', {
    hour: '2-digit',
    minute: '2-digit'
  })
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString + 'T00:00:00')
  return date.toLocaleDateString('de-DE', {
    day: '2-digit',
    month: '2-digit'
  })
}
</script>

<style scoped>
.week-view {
  overflow-x: auto;
  min-height: 600px;
}

.week-grid {
  display: flex;
  min-width: 1000px;
}

.time-column {
  width: 80px;
  flex-shrink: 0;
  border-right: 1px solid #e0e0e0;
}

.time-header {
  height: 40px;
  border-bottom: 2px solid #e0e0e0;
}

.time-slot {
  height: 60px;
  padding: 4px;
  font-size: 12px;
  color: #666;
  border-bottom: 1px solid #f0f0f0;
  text-align: right;
  padding-right: 8px;
}

.day-column {
  flex: 1;
  min-width: 120px;
  border-right: 1px solid #e0e0e0;
  position: relative;
}

.day-column:last-child {
  border-right: none;
}

.day-header {
  height: 40px;
  border-bottom: 2px solid #e0e0e0;
  padding: 4px 8px;
  text-align: center;
  background: #f5f5f5;
}

.day-header.today {
  background: #e3f2fd;
  font-weight: bold;
}

.day-name {
  font-size: 14px;
  font-weight: 500;
}

.day-date {
  font-size: 12px;
  color: #666;
}

.day-events {
  position: relative;
  height: 900px;
}

.hour-slot {
  height: 60px;
  border-bottom: 1px solid #f0f0f0;
}

.event-card {
  position: absolute;
  background: #1976d2;
  color: white;
  border-radius: 4px;
  padding: 4px 6px;
  font-size: 11px;
  overflow: hidden;
  cursor: pointer;
  border-left: 3px solid #1565c0;
  box-shadow: 0 1px 3px rgba(0,0,0,0.2);
  box-sizing: border-box;
}

.event-card:hover {
  box-shadow: 0 2px 6px rgba(0,0,0,0.3);
  z-index: 10;
}

.event-rapla {
  background: #1976d2;
  border-left-color: #1565c0;
}

.event-moodle {
  background: #7b1fa2;
  border-left-color: #6a1b9a;
}

.event-manual {
  background: #0288d1;
  border-left-color: #01579b;
}

.event-learning, .event-ai_generated {
  background: #43a047;
  border-left-color: #2e7d32;
}

.event-time {
  font-size: 10px;
  opacity: 0.9;
  margin-bottom: 2px;
}

.event-title {
  font-weight: 500;
  line-height: 1.2;
  margin-bottom: 2px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.event-location {
  font-size: 10px;
  opacity: 0.8;
  display: flex;
  align-items: center;
  gap: 2px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
