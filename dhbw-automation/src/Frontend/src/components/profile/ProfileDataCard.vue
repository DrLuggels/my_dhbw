<template>
  <v-card>
    <v-card-title>
      <v-icon left>mdi-account-circle</v-icon>
      Persönliche Daten
    </v-card-title>
    <v-card-text>
      <v-form ref="formRef" v-model="valid">
        <v-text-field
          v-model="localData.firstName"
          label="Vorname"
          :rules="nameRules"
          prepend-icon="mdi-account"
          :readonly="!editing"
          variant="outlined"
          class="mb-3"
        ></v-text-field>

        <v-text-field
          v-model="localData.lastName"
          label="Nachname"
          :rules="nameRules"
          prepend-icon="mdi-account"
          :readonly="!editing"
          variant="outlined"
          class="mb-3"
        ></v-text-field>

        <v-text-field
          v-model="localData.email"
          label="E-Mail"
          :rules="emailRules"
          prepend-icon="mdi-email"
          :readonly="!editing"
          variant="outlined"
          type="email"
          class="mb-3"
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
            :disabled="!valid"
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

interface ProfileData {
  firstName: string
  lastName: string
  email: string
}

interface Props {
  profileData: ProfileData
  saving?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  saving: false
})

const emit = defineEmits<{
  save: [data: ProfileData]
  cancel: []
}>()

const formRef = ref()
const valid = ref(false)
const editing = ref(false)
const localData = ref<ProfileData>({ ...props.profileData })

watch(() => props.profileData, (newData) => {
  localData.value = { ...newData }
  editing.value = false
}, { deep: true })

const nameRules = [
  (v: string) => !!v || 'Name ist erforderlich',
  (v: string) => (v && v.length >= 2) || 'Name muss mindestens 2 Zeichen lang sein'
]

const emailRules = [
  (v: string) => !!v || 'E-Mail ist erforderlich',
  (v: string) => /.+@.+\..+/.test(v) || 'E-Mail muss gültig sein'
]

const handleSave = () => {
  if (valid.value) {
    emit('save', localData.value)
  }
}

const handleCancel = () => {
  localData.value = { ...props.profileData }
  editing.value = false
  emit('cancel')
}
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
</style>
