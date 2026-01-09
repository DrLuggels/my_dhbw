<template>
  <v-card>
    <v-card-title>
      <v-icon left>mdi-key-variant</v-icon>
      API Keys
      <v-spacer></v-spacer>
      <v-chip size="small" color="warning" variant="outlined">
        <v-icon start size="x-small">mdi-shield-lock</v-icon>
        Vertraulich
      </v-chip>
    </v-card-title>
    <v-card-text>
      <v-form ref="formRef" v-model="valid">
        <v-alert type="info" variant="tonal" class="mb-4">
          <v-icon left>mdi-information</v-icon>
          Diese Keys werden sicher gespeichert und nur für deine KI-Funktionen verwendet.
        </v-alert>

        <v-text-field
          v-model="localKeys.openai"
          label="OpenAI API Key (ChatGPT)"
          prepend-icon="mdi-robot"
          :type="showKeys.openai ? 'text' : 'password'"
          :append-inner-icon="showKeys.openai ? 'mdi-eye-off' : 'mdi-eye'"
          @click:append-inner="showKeys.openai = !showKeys.openai"
          :readonly="!editing"
          variant="outlined"
          class="mb-3"
          hint="Beginnt mit sk-..."
          persistent-hint
        ></v-text-field>

        <v-text-field
          v-model="localKeys.anthropic"
          label="Anthropic API Key (Claude)"
          prepend-icon="mdi-robot-outline"
          :type="showKeys.anthropic ? 'text' : 'password'"
          :append-inner-icon="showKeys.anthropic ? 'mdi-eye-off' : 'mdi-eye'"
          @click:append-inner="showKeys.anthropic = !showKeys.anthropic"
          :readonly="!editing"
          variant="outlined"
          class="mb-3"
          hint="Beginnt mit sk-ant-..."
          persistent-hint
        ></v-text-field>

        <v-text-field
          v-model="localKeys.gemini"
          label="Google Gemini API Key"
          prepend-icon="mdi-google"
          :type="showKeys.gemini ? 'text' : 'password'"
          :append-inner-icon="showKeys.gemini ? 'mdi-eye-off' : 'mdi-eye'"
          @click:append-inner="showKeys.gemini = !showKeys.gemini"
          :readonly="!editing"
          variant="outlined"
          class="mb-3"
          hint="Google Cloud API Key"
          persistent-hint
        ></v-text-field>

        <div class="d-flex gap-2">
          <v-btn
            v-if="!editing"
            color="primary"
            @click="editing = true"
            block
          >
            <v-icon left>mdi-pencil</v-icon>
            Bearbeiten
          </v-btn>

          <v-btn
            v-if="editing"
            color="success"
            @click="handleSave"
            :loading="saving"
          >
            <v-icon left>mdi-content-save</v-icon>
            Speichern
          </v-btn>

          <v-btn
            v-if="editing"
            color="error"
            @click="handleCancel"
          >
            Abbrechen
          </v-btn>
        </div>
      </v-form>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'

interface ApiKeys {
  openai: string
  anthropic: string
  gemini: string
}

interface Props {
  apiKeys: ApiKeys
  saving?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  saving: false
})

const emit = defineEmits<{
  save: [keys: ApiKeys]
  cancel: []
}>()

const formRef = ref()
const valid = ref(false)
const editing = ref(false)
const localKeys = ref<ApiKeys>({ ...props.apiKeys })

const showKeys = ref({
  openai: false,
  anthropic: false,
  gemini: false
})

watch(() => props.apiKeys, (newKeys) => {
  localKeys.value = { ...newKeys }
  editing.value = false
}, { deep: true })

const handleSave = () => {
  emit('save', localKeys.value)
}

const handleCancel = () => {
  localKeys.value = { ...props.apiKeys }
  editing.value = false
  emit('cancel')
}
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
</style>
