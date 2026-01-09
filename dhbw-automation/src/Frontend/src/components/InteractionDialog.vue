<template>
  <v-card v-if="interaction" class="interaction-card elevation-4 mb-4">
    <v-card-title class="d-flex align-center">
      <v-icon left color="primary">mdi-chat-question</v-icon>
      <span class="text-h6">{{ getInteractionTitle(interaction.interactionType) }}</span>
      <v-spacer />
      <v-btn icon size="small" @click="dismissInteraction" variant="text">
        <v-icon>mdi-close</v-icon>
      </v-btn>
    </v-card-title>

    <v-card-text>
      <p class="text-body-1 mb-4">{{ interaction.question }}</p>

      <!-- Suggested Options as Chips -->
      <div v-if="parsedOptions && parsedOptions.length > 0" class="options-container mb-4">
        <v-chip
          v-for="(option, index) in parsedOptions"
          :key="index"
          class="ma-1"
          :color="selectedOption === option ? 'primary' : 'default'"
          @click="selectOption(option)"
          clickable
        >
          <v-icon left>mdi-check-circle-outline</v-icon>
          {{ option }}
        </v-chip>
      </div>

      <!-- Custom Input -->
      <v-text-field
        v-model="customAnswer"
        label="Oder eigene Antwort eingeben..."
        variant="outlined"
        density="comfortable"
        hide-details
      />
    </v-card-text>

    <v-card-actions class="px-4 pb-4">
      <v-btn
        color="grey-darken-1"
        variant="text"
        @click="snoozeInteraction"
      >
        <v-icon left>mdi-clock-outline</v-icon>
        Morgen nochmal fragen
      </v-btn>
      <v-spacer />
      <v-btn
        color="primary"
        variant="elevated"
        @click="submitAnswer"
        :disabled="!selectedOption && !customAnswer"
        :loading="loading"
      >
        Bestätigen
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import axios from 'axios'

interface Interaction {
  id: number
  interactionType: string
  question: string
  suggestedOptions?: string
  context: string
  status: string
}

const props = defineProps<{
  interaction: Interaction | null
}>()

const emit = defineEmits<{
  answered: []
  dismissed: []
}>()

const selectedOption = ref<string | null>(null)
const customAnswer = ref('')
const loading = ref(false)

const parsedOptions = computed(() => {
  if (!props.interaction?.suggestedOptions) return []
  try {
    return JSON.parse(props.interaction.suggestedOptions)
  } catch {
    return []
  }
})

const selectOption = (option: string) => {
  selectedOption.value = option
  customAnswer.value = ''
}

const submitAnswer = async () => {
  if (!props.interaction) return

  const answer = customAnswer.value || selectedOption.value
  if (!answer) return

  loading.value = true
  try {
    await axios.post(`/api/interaction/${props.interaction.id}/respond`, {
      action: 'answer',
      answer: answer
    })

    emit('answered')
  } catch (error) {
    console.error('Error submitting answer:', error)
    alert('Fehler beim Speichern der Antwort')
  } finally {
    loading.value = false
  }
}

const snoozeInteraction = async () => {
  if (!props.interaction) return

  loading.value = true
  try {
    await axios.post(`/api/interaction/${props.interaction.id}/respond`, {
      action: 'snooze',
      snoozeDays: 1
    })

    emit('answered')
  } catch (error) {
    console.error('Error snoozing interaction:', error)
    alert('Fehler beim Verschieben')
  } finally {
    loading.value = false
  }
}

const dismissInteraction = async () => {
  if (!props.interaction) return

  if (confirm('Wirklich verwerfen? Diese Aktion kann nicht rückgängig gemacht werden.')) {
    loading.value = true
    try {
      await axios.post(`/api/interaction/${props.interaction.id}/respond`, {
        action: 'dismiss'
      })

      emit('dismissed')
    } catch (error) {
      console.error('Error dismissing interaction:', error)
      alert('Fehler beim Verwerfen')
    } finally {
      loading.value = false
    }
  }
}

const getInteractionTitle = (type: string) => {
  const titles: Record<string, string> = {
    'schedule_meeting': '📅 Termin planen',
    'new_project': '🚀 Neues Projekt',
    'schedule_learning': '📚 Lernzeit einplanen',
    'acknowledge_deficit': '⚠️ Lerndefizit erkannt'
  }
  return titles[type] || '❓ Frage'
}
</script>

<style scoped>
.interaction-card {
  border-left: 4px solid rgb(var(--v-theme-primary));
  background: rgb(var(--v-theme-surface));
}

.options-container {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.v-chip {
  cursor: pointer;
  transition: all 0.2s;
}

.v-chip:hover {
  transform: scale(1.05);
}
</style>
