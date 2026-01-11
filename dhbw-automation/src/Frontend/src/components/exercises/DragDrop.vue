<template>
  <div class="drag-drop-container">
    <!-- Available Items (Source) -->
    <div class="source-area mb-4">
      <div class="text-caption text-medium-emphasis mb-2">Verfugbare Elemente:</div>
      <div class="draggable-items">
        <div
          v-for="item in availableItems"
          :key="item.id"
          class="draggable-item"
          :class="{ 'dragging': draggedItem?.id === item.id }"
          draggable="true"
          @dragstart="onDragStart($event, item)"
          @dragend="onDragEnd"
          @touchstart="onTouchStart($event, item)"
          @touchmove="onTouchMove"
          @touchend="onTouchEnd"
        >
          <v-icon size="small" class="drag-handle mr-2">mdi-drag</v-icon>
          <span v-html="item.content" />
        </div>
        <div v-if="!availableItems.length" class="text-caption text-medium-emphasis pa-4 text-center">
          Alle Elemente zugeordnet
        </div>
      </div>
    </div>

    <!-- Drop Zones -->
    <div class="drop-zones" :class="{ 'mobile-layout': isMobile }">
      <div
        v-for="zone in dropZones"
        :key="zone.id"
        class="drop-zone"
        :class="{ 'drag-over': dragOverZone === zone.id, 'has-items': zoneItems[zone.id]?.length }"
        @dragover.prevent="onDragOver($event, zone.id)"
        @dragleave="onDragLeave"
        @drop="onDrop($event, zone.id)"
      >
        <div class="zone-label">{{ zone.label }}</div>
        <div class="zone-content">
          <div
            v-for="item in zoneItems[zone.id] || []"
            :key="item.id"
            class="dropped-item"
            draggable="true"
            @dragstart="onDragStart($event, item, zone.id)"
            @dragend="onDragEnd"
          >
            <span v-html="item.content" />
            <v-btn
              v-if="!disabled"
              icon
              size="x-small"
              variant="text"
              class="remove-btn"
              @click="removeFromZone(zone.id, item)"
            >
              <v-icon size="small">mdi-close</v-icon>
            </v-btn>
          </div>
          <div v-if="!(zoneItems[zone.id]?.length)" class="zone-placeholder">
            Hierher ziehen
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useDisplay } from 'vuetify'

interface DraggableItem {
  id: string
  content: string
  category?: string
}

interface DropZone {
  id: string
  label: string
  acceptedItems?: string[]
  maxItems?: number
}

interface Props {
  config: {
    config?: {
      mode?: 'sort' | 'categorize' | 'match' | 'fill_slots'
      allowReuse?: boolean
    }
    draggables?: DraggableItem[]
    dropZones?: DropZone[]
  }
  disabled?: boolean
  modelValue?: Record<string, string[]>
}

const props = defineProps<Props>()
const emit = defineEmits(['update:modelValue', 'change'])

const { mobile: isMobile } = useDisplay()

// State
const draggedItem = ref<DraggableItem | null>(null)
const dragSourceZone = ref<string | null>(null)
const dragOverZone = ref<string | null>(null)
const zoneItems = ref<Record<string, DraggableItem[]>>({})

// Touch drag state
const _touchDragElement = ref<HTMLElement | null>(null)
const _touchStartPos = ref({ x: 0, y: 0 })

const dropZones = computed(() => props.config?.dropZones || [])
const allItems = computed(() => props.config?.draggables || [])

const availableItems = computed(() => {
  if (props.config?.config?.allowReuse) {
    return allItems.value
  }
  const placedIds = new Set(
    Object.values(zoneItems.value).flat().map(i => i.id)
  )
  return allItems.value.filter(item => !placedIds.has(item.id))
})

// Drag & Drop handlers
function onDragStart(e: DragEvent, item: DraggableItem, sourceZone?: string) {
  if (props.disabled) return
  draggedItem.value = item
  dragSourceZone.value = sourceZone || null
  e.dataTransfer!.effectAllowed = 'move'
}

function onDragEnd() {
  draggedItem.value = null
  dragSourceZone.value = null
  dragOverZone.value = null
}

function onDragOver(e: DragEvent, zoneId: string) {
  if (props.disabled || !draggedItem.value) return
  e.preventDefault()
  dragOverZone.value = zoneId
}

function onDragLeave() {
  dragOverZone.value = null
}

function onDrop(e: DragEvent, zoneId: string) {
  if (props.disabled || !draggedItem.value) return
  e.preventDefault()

  const zone = dropZones.value.find(z => z.id === zoneId)
  if (!zone) return

  // Check max items
  if (zone.maxItems && (zoneItems.value[zoneId]?.length || 0) >= zone.maxItems) {
    return
  }

  // Remove from source zone if moving between zones
  if (dragSourceZone.value) {
    zoneItems.value[dragSourceZone.value] = (zoneItems.value[dragSourceZone.value] || [])
      .filter(i => i.id !== draggedItem.value!.id)
  }

  // Add to new zone
  if (!zoneItems.value[zoneId]) {
    zoneItems.value[zoneId] = []
  }

  // Check if already in zone
  if (!zoneItems.value[zoneId].some(i => i.id === draggedItem.value!.id)) {
    zoneItems.value[zoneId].push({ ...draggedItem.value })
  }

  emitChange()
  onDragEnd()
}

function removeFromZone(zoneId: string, item: DraggableItem) {
  if (props.disabled) return
  zoneItems.value[zoneId] = (zoneItems.value[zoneId] || []).filter(i => i.id !== item.id)
  emitChange()
}

// Touch handlers for mobile
function onTouchStart(e: TouchEvent, item: DraggableItem) {
  if (props.disabled) return
  draggedItem.value = item
  touchStartPos.value = {
    x: e.touches[0].clientX,
    y: e.touches[0].clientY
  }
}

function onTouchMove(e: TouchEvent) {
  if (!draggedItem.value) return
  e.preventDefault()

  const touch = e.touches[0]
  const element = document.elementFromPoint(touch.clientX, touch.clientY)

  // Find drop zone under finger
  const zoneElement = element?.closest('.drop-zone')
  if (zoneElement) {
    const zoneId = dropZones.value.find(z =>
      zoneElement.querySelector('.zone-label')?.textContent === z.label
    )?.id
    if (zoneId) dragOverZone.value = zoneId
  } else {
    dragOverZone.value = null
  }
}

function onTouchEnd(e: TouchEvent) {
  if (!draggedItem.value) return

  const touch = e.changedTouches[0]
  const element = document.elementFromPoint(touch.clientX, touch.clientY)
  const zoneElement = element?.closest('.drop-zone')

  if (zoneElement) {
    const zone = dropZones.value.find(z =>
      zoneElement.querySelector('.zone-label')?.textContent === z.label
    )
    if (zone) {
      // Simulate drop
      if (!zoneItems.value[zone.id]) {
        zoneItems.value[zone.id] = []
      }
      if (!zoneItems.value[zone.id].some(i => i.id === draggedItem.value!.id)) {
        zoneItems.value[zone.id].push({ ...draggedItem.value })
      }
      emitChange()
    }
  }

  draggedItem.value = null
  dragOverZone.value = null
}

function emitChange() {
  const value: Record<string, string[]> = {}
  for (const [zoneId, items] of Object.entries(zoneItems.value)) {
    value[zoneId] = items.map(i => i.id)
  }
  emit('update:modelValue', value)
  emit('change', value)
}

// Initialize from modelValue
watch(() => props.modelValue, (val) => {
  if (!val) return
  const newZoneItems: Record<string, DraggableItem[]> = {}
  for (const [zoneId, itemIds] of Object.entries(val)) {
    newZoneItems[zoneId] = itemIds
      .map(id => allItems.value.find(i => i.id === id))
      .filter(Boolean) as DraggableItem[]
  }
  zoneItems.value = newZoneItems
}, { immediate: true })

onMounted(() => {
  // Initialize empty zones
  for (const zone of dropZones.value) {
    if (!zoneItems.value[zone.id]) {
      zoneItems.value[zone.id] = []
    }
  }
})
</script>

<style scoped>
.drag-drop-container {
  width: 100%;
}

.source-area {
  background: rgba(var(--v-theme-surface-variant), 0.5);
  border-radius: 12px;
  padding: 12px;
}

.draggable-items {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  min-height: 48px;
}

.draggable-item {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  background: rgb(var(--v-theme-surface));
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 8px;
  cursor: grab;
  user-select: none;
  transition: all 0.2s ease;
  touch-action: none;
}

.draggable-item:hover {
  border-color: rgb(var(--v-theme-primary));
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.draggable-item.dragging {
  opacity: 0.5;
  transform: scale(0.95);
}

.drag-handle {
  color: rgba(var(--v-theme-on-surface), 0.4);
}

.drop-zones {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
}

.drop-zones.mobile-layout {
  grid-template-columns: 1fr;
}

.drop-zone {
  border: 2px dashed rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 12px;
  padding: 12px;
  min-height: 120px;
  transition: all 0.2s ease;
}

.drop-zone.drag-over {
  border-color: rgb(var(--v-theme-primary));
  background: rgba(var(--v-theme-primary), 0.04);
}

.drop-zone.has-items {
  border-style: solid;
  border-color: rgba(var(--v-theme-primary), 0.3);
}

.zone-label {
  font-weight: 600;
  font-size: 0.9rem;
  margin-bottom: 8px;
  color: rgb(var(--v-theme-primary));
}

.zone-content {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.zone-placeholder {
  color: rgba(var(--v-theme-on-surface), 0.4);
  font-size: 0.875rem;
  text-align: center;
  padding: 16px;
}

.dropped-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 12px;
  background: rgba(var(--v-theme-primary), 0.1);
  border: 1px solid rgba(var(--v-theme-primary), 0.3);
  border-radius: 8px;
  cursor: grab;
}

.remove-btn {
  opacity: 0.6;
  margin-left: 8px;
}

.remove-btn:hover {
  opacity: 1;
}

/* Mobile optimizations */
@media (max-width: 600px) {
  .draggable-item {
    padding: 12px 16px;
    font-size: 0.95rem;
  }

  .drop-zone {
    min-height: 100px;
    padding: 16px;
  }
}
</style>
