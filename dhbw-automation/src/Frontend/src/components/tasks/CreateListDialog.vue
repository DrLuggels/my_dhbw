<template>
  <v-dialog v-model="dialog" max-width="400" persistent>
    <v-card>
      <v-card-title>Neue Liste erstellen</v-card-title>

      <v-card-text>
        <v-text-field
          v-model="name"
          label="Name"
          placeholder="z.B. Ministranten, Arbeit, Einkauf..."
          variant="outlined"
          :error-messages="nameError"
          autofocus
          @keyup.enter="submit"
        />

        <v-select
          v-model="icon"
          :items="iconOptions"
          label="Icon"
          variant="outlined"
        >
          <template v-slot:item="{ props, item }">
            <v-list-item v-bind="props">
              <template v-slot:prepend>
                <v-icon>{{ item.value }}</v-icon>
              </template>
            </v-list-item>
          </template>
          <template v-slot:selection="{ item }">
            <v-icon class="mr-2">{{ item.value }}</v-icon>
            {{ item.title }}
          </template>
        </v-select>

        <v-label class="mb-2">Farbe</v-label>
        <div class="d-flex flex-wrap ga-2">
          <v-btn
            v-for="c in colorOptions"
            :key="c"
            icon
            size="small"
            :color="c"
            :variant="color === c ? 'flat' : 'outlined'"
            @click="color = c"
          >
            <v-icon v-if="color === c">mdi-check</v-icon>
          </v-btn>
        </div>
      </v-card-text>

      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="close">Abbrechen</v-btn>
        <v-btn
          color="primary"
          variant="flat"
          @click="submit"
          :loading="isLoading"
          :disabled="!name.trim()"
        >
          Erstellen
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'

const dialog = defineModel<boolean>({ default: false })

const emit = defineEmits<{
  (e: 'created', data: { name: string; icon: string; color: string }): void
}>()

const name = ref('')
const icon = ref('mdi-checkbox-marked-circle-outline')
const color = ref('#1976D2')
const nameError = ref('')
const isLoading = ref(false)

const iconOptions = [
  { title: 'Standard', value: 'mdi-checkbox-marked-circle-outline' },
  { title: 'Arbeit', value: 'mdi-briefcase' },
  { title: 'Studium', value: 'mdi-school' },
  { title: 'Einkauf', value: 'mdi-cart' },
  { title: 'Sport', value: 'mdi-run' },
  { title: 'Zuhause', value: 'mdi-home' },
  { title: 'Reise', value: 'mdi-airplane' },
  { title: 'Gesundheit', value: 'mdi-heart' },
  { title: 'Finanzen', value: 'mdi-currency-eur' },
  { title: 'Hobby', value: 'mdi-palette' },
  { title: 'Familie', value: 'mdi-account-group' },
  { title: 'Projekt', value: 'mdi-folder' },
  { title: 'Kirche', value: 'mdi-church' },
  { title: 'Auto', value: 'mdi-car' },
  { title: 'Telefon', value: 'mdi-phone' }
]

const colorOptions = [
  '#1976D2', // Blue
  '#4CAF50', // Green
  '#FF9800', // Orange
  '#E91E63', // Pink
  '#9C27B0', // Purple
  '#00BCD4', // Cyan
  '#795548', // Brown
  '#607D8B', // Blue Grey
  '#F44336', // Red
  '#FFEB3B'  // Yellow
]

watch(dialog, (newVal) => {
  if (!newVal) {
    // Reset on close
    name.value = ''
    icon.value = 'mdi-checkbox-marked-circle-outline'
    color.value = '#1976D2'
    nameError.value = ''
  }
})

function close() {
  dialog.value = false
}

async function submit() {
  if (!name.value.trim()) {
    nameError.value = 'Name ist erforderlich'
    return
  }

  nameError.value = ''
  isLoading.value = true

  try {
    emit('created', {
      name: name.value.trim(),
      icon: icon.value,
      color: color.value
    })
    close()
  } finally {
    isLoading.value = false
  }
}
</script>
