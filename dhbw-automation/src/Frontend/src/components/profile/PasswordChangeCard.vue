<template>
  <v-card>
    <v-card-title>
      <v-icon left>mdi-lock</v-icon>
      Passwort ändern
    </v-card-title>
    <v-card-text>
      <v-form ref="formRef" v-model="valid">
        <v-text-field
          v-model="formData.currentPassword"
          label="Aktuelles Passwort"
          :rules="requiredRules"
          prepend-icon="mdi-lock"
          type="password"
          variant="outlined"
          class="mb-3"
        ></v-text-field>

        <v-text-field
          v-model="formData.newPassword"
          label="Neues Passwort"
          :rules="passwordRules"
          prepend-icon="mdi-lock-plus"
          type="password"
          variant="outlined"
          class="mb-3"
        ></v-text-field>

        <v-text-field
          v-model="formData.confirmPassword"
          label="Passwort bestätigen"
          :rules="[...requiredRules, passwordMatchRule]"
          prepend-icon="mdi-lock-check"
          type="password"
          variant="outlined"
          class="mb-3"
        ></v-text-field>

        <v-btn
          color="primary"
          @click="handleSubmit"
          :loading="loading"
          :disabled="!valid"
          block
        >
          <v-icon left>mdi-key-change</v-icon>
          Passwort ändern
        </v-btn>
      </v-form>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { ref } from 'vue'

interface Props {
  loading?: boolean
}

withDefaults(defineProps<Props>(), {
  loading: false
})

const emit = defineEmits<{
  change: [currentPassword: string, newPassword: string]
}>()

const formRef = ref()
const valid = ref(false)

const formData = ref({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const requiredRules = [
  (v: string) => !!v || 'Dieses Feld ist erforderlich'
]

const passwordRules = [
  (v: string) => !!v || 'Passwort ist erforderlich',
  (v: string) => (v && v.length >= 6) || 'Passwort muss mindestens 6 Zeichen lang sein'
]

const passwordMatchRule = (v: string) => {
  return v === formData.value.newPassword || 'Passwörter stimmen nicht überein'
}

const handleSubmit = () => {
  if (valid.value) {
    emit('change', formData.value.currentPassword, formData.value.newPassword)
  }
}

defineExpose({
  reset: () => {
    formData.value = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    }
    formRef.value?.reset()
  }
})
</script>
