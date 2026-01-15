<template>
  <v-container class="pa-4">
    <v-card class="mb-4">
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2">mdi-test-tube</v-icon>
        Interactive Component Test Page
      </v-card-title>
      <v-card-text>
        Diese Seite testet alle interaktiven Komponenten ohne API-Aufrufe.
      </v-card-text>
    </v-card>

    <!-- Test Mode Toggle -->
    <v-card class="mb-4">
      <v-card-text>
        <v-btn-toggle v-model="testMode" mandatory color="primary" variant="outlined">
          <v-btn value="components">Einzelne Komponenten</v-btn>
          <v-btn value="player">Kompletter Player</v-btn>
        </v-btn-toggle>
      </v-card-text>
    </v-card>

    <!-- Single Component Tests -->
    <template v-if="testMode === 'components'">
      <!-- Multiple Choice Test -->
      <v-card class="mb-4">
        <v-card-title>
          <v-icon class="mr-2">mdi-radiobox-marked</v-icon>
          Multiple Choice
        </v-card-title>
        <v-card-text>
          <p class="mb-4 text-body-1">Was ist die Ableitung von x^2?</p>
          <MultipleChoice
            :config="multipleChoiceConfig"
            v-model="multipleChoiceAnswer"
          />
          <div class="mt-4">
            <v-chip color="info" variant="tonal">
              Antwort: {{ multipleChoiceAnswer || 'keine' }}
            </v-chip>
          </div>
        </v-card-text>
      </v-card>

      <!-- Multiple Choice (Multi-Select) Test -->
      <v-card class="mb-4">
        <v-card-title>
          <v-icon class="mr-2">mdi-checkbox-marked-outline</v-icon>
          Multiple Choice (Mehrfachauswahl)
        </v-card-title>
        <v-card-text>
          <p class="mb-4 text-body-1">Welche der folgenden sind Primzahlen?</p>
          <MultipleChoice
            :config="multipleChoiceMultiConfig"
            v-model="multipleChoiceMultiAnswer"
          />
          <div class="mt-4">
            <v-chip color="info" variant="tonal">
              Antworten: {{ multipleChoiceMultiAnswer?.join(', ') || 'keine' }}
            </v-chip>
          </div>
        </v-card-text>
      </v-card>

      <!-- Drag & Drop Test -->
      <v-card class="mb-4">
        <v-card-title>
          <v-icon class="mr-2">mdi-drag</v-icon>
          Drag & Drop
        </v-card-title>
        <v-card-text>
          <p class="mb-4 text-body-1">Ordne die Begriffe den richtigen Kategorien zu:</p>
          <DragDrop
            :config="dragDropConfig"
            v-model="dragDropAnswer"
          />
          <div class="mt-4">
            <v-chip color="info" variant="tonal" class="mr-2" v-for="(items, zone) in dragDropAnswer" :key="zone">
              {{ zone }}: {{ items.join(', ') || 'leer' }}
            </v-chip>
          </div>
        </v-card-text>
      </v-card>

      <!-- Fill in Blank Test -->
      <v-card class="mb-4">
        <v-card-title>
          <v-icon class="mr-2">mdi-form-textbox</v-icon>
          Fill in Blank
        </v-card-title>
        <v-card-text>
          <p class="mb-4 text-body-1">Erganze die Lucken:</p>
          <FillInBlank
            :config="fillBlankConfig"
            v-model="fillBlankAnswer"
          />
          <div class="mt-4">
            <v-chip color="info" variant="tonal">
              Antwort: {{ JSON.stringify(fillBlankAnswer) || 'keine' }}
            </v-chip>
          </div>
        </v-card-text>
      </v-card>

      <!-- Text Input Test -->
      <v-card class="mb-4">
        <v-card-title>
          <v-icon class="mr-2">mdi-text</v-icon>
          Text Input
        </v-card-title>
        <v-card-text>
          <p class="mb-4 text-body-1">Erklare den Begriff "Rekursion":</p>
          <TextInput
            :config="textInputConfig"
            v-model="textInputAnswer"
          />
          <div class="mt-4">
            <v-chip color="info" variant="tonal">
              Antwort: {{ textInputAnswer || 'keine' }}
            </v-chip>
          </div>
        </v-card-text>
      </v-card>

      <!-- Slider Range (simulated as TextInput) Test -->
      <v-card class="mb-4">
        <v-card-title>
          <v-icon class="mr-2">mdi-ray-vertex</v-icon>
          Slider Range (als TextInput)
        </v-card-title>
        <v-card-text>
          <p class="mb-4 text-body-1">Was ist das Ergebnis von 7 * 8?</p>
          <TextInput
            :config="sliderConfig"
            v-model="sliderAnswer"
          />
          <div class="mt-4">
            <v-chip color="info" variant="tonal">
              Antwort: {{ sliderAnswer || 'keine' }}
            </v-chip>
          </div>
        </v-card-text>
      </v-card>
    </template>

    <!-- Full Player Test -->
    <template v-else>
      <InteractiveExercisePlayer
        :exercise="mockExercise"
        @complete="onComplete"
        @progress="onProgress"
      />

      <v-card class="mt-4">
        <v-card-title>Debug Log</v-card-title>
        <v-card-text>
          <pre class="text-caption">{{ debugLog }}</pre>
        </v-card-text>
      </v-card>
    </template>
  </v-container>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import MultipleChoice from '@/components/exercises/MultipleChoice.vue'
import DragDrop from '@/components/exercises/DragDrop.vue'
import FillInBlank from '@/components/exercises/FillInBlank.vue'
import TextInput from '@/components/exercises/TextInput.vue'
import InteractiveExercisePlayer from '@/components/exercises/InteractiveExercisePlayer.vue'

const testMode = ref('components')

// Component answers
const multipleChoiceAnswer = ref<string>('')
const multipleChoiceMultiAnswer = ref<string[]>([])
const dragDropAnswer = ref<Record<string, string[]>>({})
const fillBlankAnswer = ref<string | Record<string, string>>('')
const textInputAnswer = ref('')
const sliderAnswer = ref('')

// Debug log for player
const debugLog = ref('')

// Multiple Choice Config (Single)
const multipleChoiceConfig = {
  type: 'multiple_choice',
  options: [
    { id: 'opt1', label: '2x', isCorrect: true, explanation: 'Die Potenzregel: d/dx(x^n) = n*x^(n-1)' },
    { id: 'opt2', label: 'x^2', isCorrect: false, explanation: 'Das ist die ursprungliche Funktion, nicht die Ableitung' },
    { id: 'opt3', label: '2', isCorrect: false, explanation: 'Das ware die zweite Ableitung' },
    { id: 'opt4', label: 'x', isCorrect: false, explanation: 'Falscher Exponent' }
  ]
}

// Multiple Choice Config (Multi-Select)
const multipleChoiceMultiConfig = {
  type: 'multiple_choice',
  config: {
    allowMultiple: true
  },
  options: [
    { id: 'num2', label: '2', isCorrect: true },
    { id: 'num3', label: '3', isCorrect: true },
    { id: 'num4', label: '4', isCorrect: false },
    { id: 'num7', label: '7', isCorrect: true }
  ]
}

// Drag & Drop Config
const dragDropConfig = {
  type: 'drag_drop',
  draggables: [
    { id: 'item1', content: 'Python' },
    { id: 'item2', content: 'JavaScript' },
    { id: 'item3', content: 'MySQL' },
    { id: 'item4', content: 'PostgreSQL' },
    { id: 'item5', content: 'TypeScript' },
    { id: 'item6', content: 'MongoDB' }
  ],
  dropZones: [
    { id: 'zone1', label: 'Programmiersprachen', acceptedItems: ['item1', 'item2', 'item5'] },
    { id: 'zone2', label: 'Datenbanken', acceptedItems: ['item3', 'item4', 'item6'] }
  ]
}

// Fill in Blank Config
const fillBlankConfig = {
  type: 'fill_blank',
  template: 'In Python verwendet man <code>{{blank:blank1}}</code> um eine Funktion zu definieren, und <code>{{blank:blank2}}</code> um einen Wert zuruckzugeben.',
  blanks: [
    { id: 'blank1', correctAnswers: ['def'], hint: 'Beginnt mit "d"' },
    { id: 'blank2', correctAnswers: ['return'], hint: 'Englisches Wort fur "zuruckgeben"' }
  ],
  correctAnswer: 'def'
}

// Text Input Config
const textInputConfig = {
  type: 'text_input',
  config: {
    multiline: true,
    rows: 3,
    placeholder: 'Deine Erklarung hier...'
  }
}

// Slider Config (shown as TextInput fallback)
const sliderConfig = {
  type: 'slider_range',
  config: {
    placeholder: 'Gib eine Zahl ein...',
    correctValue: 56,
    tolerance: 0
  }
}

// Mock Exercise for Player
const mockExercise = {
  id: 9999,
  subject: 'Mathematik',
  topic: 'Grundrechenarten',
  difficulty: 'easy',
  score: 0,
  completedSteps: 0,
  totalSteps: 3,
  stepProgress: JSON.stringify({ steps: {} }),
  exerciseContent: JSON.stringify({
    version: '2.0',
    metadata: {
      subject: 'Mathematik',
      topic: 'Grundrechenarten',
      difficulty: 'easy',
      estimatedMinutes: 5
    },
    steps: [
      {
        id: 'step-1',
        order: 1,
        title: 'Multiple Choice Test',
        instruction: 'Wahle die richtige Antwort aus.',
        component: {
          type: 'multiple_choice',
          options: [
            { id: 'a', label: '10', isCorrect: false },
            { id: 'b', label: '12', isCorrect: true },
            { id: 'c', label: '14', isCorrect: false }
          ]
        },
        feedback: {
          onCorrect: { message: 'Richtig! 3 + 9 = 12' },
          onIncorrect: { message: 'Leider falsch. Versuche es nochmal.' }
        },
        hints: [
          { order: 1, content: 'Zahle mit den Fingern' }
        ]
      },
      {
        id: 'step-2',
        order: 2,
        title: 'Drag & Drop Test',
        instruction: 'Ordne die Zahlen den Kategorien zu.',
        component: {
          type: 'drag_drop',
          draggables: [
            { id: 'd1', content: '2' },
            { id: 'd2', content: '3' },
            { id: 'd3', content: '4' },
            { id: 'd4', content: '6' }
          ],
          dropZones: [
            { id: 'gerade', label: 'Gerade Zahlen', acceptedItems: ['d1', 'd3', 'd4'] },
            { id: 'ungerade', label: 'Ungerade Zahlen', acceptedItems: ['d2'] }
          ]
        },
        feedback: {
          onCorrect: { message: 'Perfekt!' },
          onIncorrect: { message: 'Nicht ganz richtig.' }
        },
        hints: []
      },
      {
        id: 'step-3',
        order: 3,
        title: 'Text Eingabe',
        instruction: 'Was ist 7 * 8? Gib das Ergebnis ein.',
        component: {
          type: 'slider_range',
          config: {
            correctValue: 56,
            tolerance: 0
          }
        },
        feedback: {
          onCorrect: { message: 'Richtig! 7 * 8 = 56' },
          onIncorrect: { message: 'Das stimmt leider nicht.' }
        },
        hints: [
          { order: 1, content: '7 * 8 ist etwas mehr als 50' }
        ]
      }
    ]
  })
}

function onComplete(exercise: unknown) {
  debugLog.value += `\n[${new Date().toISOString()}] Complete: ${JSON.stringify(exercise, null, 2)}`
}

function onProgress(progress: unknown) {
  debugLog.value += `\n[${new Date().toISOString()}] Progress: ${JSON.stringify(progress)}`
}
</script>

<style scoped>
pre {
  background: rgba(0, 0, 0, 0.05);
  padding: 12px;
  border-radius: 8px;
  max-height: 300px;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
