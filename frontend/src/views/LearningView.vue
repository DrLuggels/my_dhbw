<script setup lang="ts">
import ExercisePlayer from '@/components/learning/ExercisePlayer.vue'
import StatsCards from '@/components/learning/StatsCards.vue'
import StreakWidget from '@/components/learning/StreakWidget.vue'
import LoadingState from '@/components/common/LoadingState.vue'
import { useLearningStore } from '@/stores/learning'
import { onMounted, ref } from 'vue'

const learning = useLearningStore()
const mode = ref<'overview' | 'exercise'>('overview')

onMounted(async () => {
  await Promise.all([learning.fetchStats(), learning.fetchStreak()])
})

async function startLearning() {
  await learning.fetchNext()
  if (learning.currentExercise) {
    mode.value = 'exercise'
  }
}

function onExerciseDone() {
  mode.value = 'overview'
  learning.fetchStats()
  learning.fetchStreak()
}
</script>

<template>
  <div>
    <v-toolbar flat color="transparent">
      <v-toolbar-title>Lernen</v-toolbar-title>
      <v-spacer />
      <v-btn
        v-if="mode === 'overview'"
        color="primary"
        prepend-icon="mdi-play"
        @click="startLearning"
      >
        Lernen starten
      </v-btn>
      <v-btn
        v-else
        variant="outlined"
        prepend-icon="mdi-arrow-left"
        @click="mode = 'overview'"
      >
        Zurück
      </v-btn>
    </v-toolbar>

    <v-container fluid class="pa-6">
      <LoadingState :loading="learning.loading" :error="learning.error">
        <template v-if="mode === 'overview'">
          <StreakWidget v-if="learning.streak" :streak="learning.streak" class="mb-6" />
          <StatsCards v-if="learning.stats" :stats="learning.stats" />
        </template>

        <ExercisePlayer
          v-else-if="learning.currentExercise"
          :exercise="learning.currentExercise"
          @answered="onExerciseDone"
          @next="startLearning"
        />
      </LoadingState>
    </v-container>
  </div>
</template>
