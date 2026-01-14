<template>
  <div>
    <!-- Stats Overview -->
    <v-row dense class="mb-4">
      <v-col cols="6" sm="3">
        <v-card class="text-center pa-4" :loading="loading">
          <v-icon size="32" color="success" class="mb-2">mdi-trophy</v-icon>
          <div class="text-h5">{{ stats?.masteredEntities || 0 }}</div>
          <div class="text-caption text-medium-emphasis">Gemeistert</div>
        </v-card>
      </v-col>
      <v-col cols="6" sm="3">
        <v-card class="text-center pa-4" :loading="loading">
          <v-icon size="32" color="warning" class="mb-2">mdi-school</v-icon>
          <div class="text-h5">{{ stats?.learningEntities || 0 }}</div>
          <div class="text-caption text-medium-emphasis">Lernend</div>
        </v-card>
      </v-col>
      <v-col cols="6" sm="3">
        <v-card class="text-center pa-4" :loading="loading">
          <v-icon size="32" color="info" class="mb-2">mdi-new-box</v-icon>
          <div class="text-h5">{{ stats?.newEntities || 0 }}</div>
          <div class="text-caption text-medium-emphasis">Neu</div>
        </v-card>
      </v-col>
      <v-col cols="6" sm="3">
        <v-card class="text-center pa-4" :loading="loading">
          <v-icon size="32" color="primary" class="mb-2">mdi-percent</v-icon>
          <div class="text-h5">{{ formatPercent(stats?.overallSuccessRate || 0) }}</div>
          <div class="text-caption text-medium-emphasis">Erfolgsrate</div>
        </v-card>
      </v-col>
    </v-row>

    <!-- Main Content Grid -->
    <v-row>
      <!-- Weak Areas -->
      <v-col cols="12" md="6">
        <v-card>
          <v-card-title class="d-flex align-center">
            <v-icon start color="error">mdi-alert-circle</v-icon>
            Schwachstellen
            <v-spacer />
            <v-btn
              icon="mdi-refresh"
              variant="text"
              size="small"
              :loading="loadingWeakAreas"
              @click="$emit('refresh-weak-areas')"
            />
          </v-card-title>
          <v-card-text>
            <div v-if="loadingWeakAreas" class="text-center py-4">
              <v-progress-circular indeterminate size="24" />
            </div>
            <div v-else-if="weakAreas.length === 0" class="text-center py-4 text-medium-emphasis">
              <v-icon size="32" color="grey">mdi-check-circle-outline</v-icon>
              <p class="mt-2">Keine Schwachstellen identifiziert!</p>
            </div>
            <v-list v-else density="compact" class="py-0">
              <v-list-item
                v-for="area in weakAreas"
                :key="area.entityId"
                @click="$emit('practice-entity', area.entityId)"
              >
                <template #prepend>
                  <v-avatar :color="getReasonColor(area.reason)" size="32">
                    <v-icon size="18" color="white">{{ getReasonIcon(area.reason) }}</v-icon>
                  </v-avatar>
                </template>
                <v-list-item-title>{{ area.entityName }}</v-list-item-title>
                <v-list-item-subtitle>
                  {{ area.subject }} - {{ getReasonLabel(area.reason) }}
                </v-list-item-subtitle>
                <template #append>
                  <div class="text-right">
                    <v-progress-linear
                      :model-value="area.masteryScore * 100"
                      :color="getMasteryColor(area.masteryScore)"
                      height="6"
                      rounded
                      style="width: 60px"
                    />
                    <div class="text-caption">{{ formatPercent(area.masteryScore) }}</div>
                  </div>
                </template>
              </v-list-item>
            </v-list>
          </v-card-text>
        </v-card>
      </v-col>

      <!-- Due for Review -->
      <v-col cols="12" md="6">
        <v-card>
          <v-card-title class="d-flex align-center">
            <v-icon start color="warning">mdi-calendar-clock</v-icon>
            Fällige Wiederholungen
            <v-spacer />
            <v-btn
              icon="mdi-refresh"
              variant="text"
              size="small"
              :loading="loadingDueReview"
              @click="$emit('refresh-due-review')"
            />
          </v-card-title>
          <v-card-text>
            <div v-if="loadingDueReview" class="text-center py-4">
              <v-progress-circular indeterminate size="24" />
            </div>
            <div v-else-if="dueForReview.length === 0" class="text-center py-4 text-medium-emphasis">
              <v-icon size="32" color="grey">mdi-calendar-check</v-icon>
              <p class="mt-2">Keine Wiederholungen fällig!</p>
            </div>
            <v-list v-else density="compact" class="py-0">
              <v-list-item
                v-for="entity in dueForReview"
                :key="entity.id"
                @click="$emit('practice-entity', entity.id)"
              >
                <template #prepend>
                  <v-avatar :color="getEntityTypeColor(entity.entityType)" size="32">
                    <v-icon size="18" color="white">{{ getEntityTypeIcon(entity.entityType) }}</v-icon>
                  </v-avatar>
                </template>
                <v-list-item-title>{{ entity.name }}</v-list-item-title>
                <v-list-item-subtitle>
                  {{ entity.subject || 'Kein Fach' }} - {{ entity.topic || '' }}
                </v-list-item-subtitle>
                <template #append>
                  <v-chip
                    size="x-small"
                    :color="getMasteryColor(entity.masteryScore || 0)"
                    variant="tonal"
                  >
                    {{ formatPercent(entity.masteryScore || 0) }}
                  </v-chip>
                </template>
              </v-list-item>
            </v-list>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <!-- Subject Breakdown -->
    <v-card class="mt-4" v-if="stats && Object.keys(stats.bySubject).length > 0">
      <v-card-title>
        <v-icon start>mdi-chart-bar</v-icon>
        Fortschritt nach Fach
      </v-card-title>
      <v-card-text>
        <v-row dense>
          <v-col
            v-for="(subject, name) in stats.bySubject"
            :key="name"
            cols="12"
            sm="6"
            md="4"
          >
            <v-card variant="outlined" class="pa-3">
              <div class="d-flex align-center mb-2">
                <span class="font-weight-medium">{{ name }}</span>
                <v-spacer />
                <span class="text-caption">
                  {{ subject.masteredEntities }}/{{ subject.totalEntities }}
                </span>
              </div>
              <v-progress-linear
                :model-value="subject.averageMastery * 100"
                :color="getMasteryColor(subject.averageMastery)"
                height="8"
                rounded
              />
              <div class="text-caption text-medium-emphasis mt-1">
                {{ formatPercent(subject.averageMastery) }} Beherrschung
              </div>
            </v-card>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <!-- Bloom Level Distribution -->
    <v-card class="mt-4" v-if="stats && Object.keys(stats.byBloomLevel).length > 0">
      <v-card-title>
        <v-icon start>mdi-stairs</v-icon>
        Leistung nach Bloom-Level
      </v-card-title>
      <v-card-text>
        <div class="d-flex flex-wrap gap-2">
          <v-chip
            v-for="(count, level) in stats.byBloomLevel"
            :key="level"
            :color="getBloomLevelColor(Number(level))"
            variant="tonal"
          >
            {{ getBloomLevelName(Number(level)) }}: {{ count }}
          </v-chip>
        </div>
      </v-card-text>
    </v-card>

    <!-- Streak Info -->
    <v-card class="mt-4" v-if="stats">
      <v-card-text>
        <div class="d-flex align-center justify-space-around">
          <div class="text-center">
            <v-icon size="32" color="orange">mdi-fire</v-icon>
            <div class="text-h5 mt-1">{{ stats.currentStreak }}</div>
            <div class="text-caption">Aktuelle Serie</div>
          </div>
          <v-divider vertical class="mx-4" />
          <div class="text-center">
            <v-icon size="32" color="amber">mdi-star</v-icon>
            <div class="text-h5 mt-1">{{ stats.bestStreak }}</div>
            <div class="text-caption">Beste Serie</div>
          </div>
          <v-divider vertical class="mx-4" />
          <div class="text-center">
            <v-icon size="32" color="primary">mdi-check-all</v-icon>
            <div class="text-h5 mt-1">{{ stats.totalAttempts }}</div>
            <div class="text-caption">Versuche</div>
          </div>
          <v-divider vertical class="mx-4" />
          <div class="text-center">
            <v-icon size="32" color="success">mdi-check-circle</v-icon>
            <div class="text-h5 mt-1">{{ stats.totalCorrect }}</div>
            <div class="text-caption">Richtig</div>
          </div>
        </div>
      </v-card-text>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import type { MasteryStats, WeakArea, KgEntity } from '@/types/learningEngine'
import { entityTypes, bloomLevels, getMasteryColor as getMasteryColorUtil } from '@/types/learningEngine'

defineProps<{
  stats: MasteryStats | null
  weakAreas: WeakArea[]
  dueForReview: KgEntity[]
  loading?: boolean
  loadingWeakAreas?: boolean
  loadingDueReview?: boolean
}>()

defineEmits<{
  'refresh-weak-areas': []
  'refresh-due-review': []
  'practice-entity': [entityId: number]
}>()

const formatPercent = (value: number): string => {
  return `${Math.round(value * 100)}%`
}

const getMasteryColor = (score: number): string => {
  return getMasteryColorUtil(score)
}

const getReasonColor = (reason: string): string => {
  switch (reason) {
    case 'low_mastery': return 'error'
    case 'overdue': return 'warning'
    case 'high_error_rate': return 'orange'
    default: return 'grey'
  }
}

const getReasonIcon = (reason: string): string => {
  switch (reason) {
    case 'low_mastery': return 'mdi-trending-down'
    case 'overdue': return 'mdi-clock-alert'
    case 'high_error_rate': return 'mdi-alert'
    default: return 'mdi-help'
  }
}

const getReasonLabel = (reason: string): string => {
  switch (reason) {
    case 'low_mastery': return 'Niedrige Beherrschung'
    case 'overdue': return 'Überfällig'
    case 'high_error_rate': return 'Hohe Fehlerrate'
    default: return reason
  }
}

const getEntityTypeColor = (type: string): string => {
  const info = entityTypes.find(t => t.value === type)
  return info?.color || 'grey'
}

const getEntityTypeIcon = (type: string): string => {
  const info = entityTypes.find(t => t.value === type)
  return info?.icon || 'mdi-help'
}

const getBloomLevelName = (level: number): string => {
  const info = bloomLevels.find(b => b.level === level)
  return info?.name || `Level ${level}`
}

const getBloomLevelColor = (level: number): string => {
  const colors = ['grey', 'green', 'blue', 'orange', 'purple', 'red']
  return colors[Math.min(level, colors.length - 1)] || 'grey'
}
</script>
