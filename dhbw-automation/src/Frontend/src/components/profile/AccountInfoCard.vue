<template>
  <v-card>
    <v-card-title>
      <v-icon left>mdi-information</v-icon>
      Account-Informationen
    </v-card-title>
    <v-card-text>
      <v-list>
        <v-list-item>
          <template v-slot:prepend>
            <v-icon>mdi-email</v-icon>
          </template>
          <v-list-item-title>E-Mail</v-list-item-title>
          <v-list-item-subtitle>{{ email }}</v-list-item-subtitle>
        </v-list-item>

        <v-divider class="my-3"></v-divider>

        <v-list-item>
          <v-btn color="error" variant="outlined" @click="emit('logout')" block>
            <v-icon left>mdi-logout</v-icon>
            Abmelden
          </v-btn>
        </v-list-item>
      </v-list>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
interface Props {
  email?: string
}

withDefaults(defineProps<Props>(), {
  email: ''
})

const emit = defineEmits<{
  logout: []
}>()
</script>
