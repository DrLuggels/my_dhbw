<template>
  <div class="travel-view">
    <v-container>
      <div class="d-flex align-center mb-4">
        <v-btn icon variant="text" @click="$router.back()" class="mr-3">
          <v-icon>mdi-arrow-left</v-icon>
        </v-btn>
        <h1 class="text-h4">
          <v-icon left color="primary">mdi-train</v-icon>
          Zugverbindungen
        </h1>
      </div>
      <v-row>
        <v-col cols="12">
          <v-card>

            <v-card-text>
              <!-- Suchformular -->
              <v-form @submit.prevent="searchConnections">
                <v-row>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model="searchRequest.from"
                      label="Von"
                      prepend-icon="mdi-map-marker"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-text-field
                      v-model="searchRequest.to"
                      label="Nach"
                      prepend-icon="mdi-map-marker-check"
                      variant="outlined"
                      density="comfortable"
                    />
                  </v-col>
                  <v-col cols="12" md="4">
                    <v-btn
                      color="primary"
                      size="large"
                      block
                      type="submit"
                      :loading="loading"
                    >
                      <v-icon left>mdi-magnify</v-icon>
                      Suchen
                    </v-btn>
                  </v-col>
                </v-row>
              </v-form>

              <v-divider class="my-6" />

              <!-- Schnellzugriff für Standard-Route -->
              <div class="mb-6">
                <v-btn
                  color="secondary"
                  variant="outlined"
                  @click="loadDefaultConnections"
                  :loading="loading"
                >
                  <v-icon left>mdi-lightning-bolt</v-icon>
                  Standard-Route laden (Laupheim West → Ravensburg)
                </v-btn>
              </div>

              <!-- Fehleranzeige -->
              <v-alert
                v-if="error"
                type="error"
                closable
                @click:close="error = null"
                class="mb-4"
              >
                {{ error }}
              </v-alert>

              <!-- Verbindungsliste -->
              <div v-if="connections && connections.journeys.length > 0">
                <div class="text-subtitle-1 mb-4">
                  {{ connections.journeys.length }} Verbindung(en) gefunden
                </div>

                <v-expansion-panels variant="accordion">
                  <v-expansion-panel
                    v-for="(journey, index) in connections.journeys"
                    :key="index"
                  >
                    <v-expansion-panel-title>
                      <v-row no-gutters align="center">
                        <v-col cols="12" md="3">
                          <div class="font-weight-bold">
                            {{ formatTime(journey.departure) }}
                          </div>
                          <div class="text-caption">{{ journey.from }}</div>
                        </v-col>
                        <v-col cols="12" md="2" class="text-center">
                          <v-icon>mdi-arrow-right</v-icon>
                          <div class="text-caption">{{ journey.duration }}</div>
                        </v-col>
                        <v-col cols="12" md="3">
                          <div class="font-weight-bold">
                            {{ formatTime(journey.arrival) }}
                          </div>
                          <div class="text-caption">{{ journey.to }}</div>
                        </v-col>
                        <v-col cols="12" md="2" class="text-center">
                          <v-chip
                            :color="journey.transfers === 0 ? 'success' : 'info'"
                            size="small"
                          >
                            {{ journey.transfers }} Umstieg{{ journey.transfers !== 1 ? 'e' : '' }}
                          </v-chip>
                        </v-col>
                        <v-col cols="12" md="2" class="text-right">
                          <v-chip
                            v-if="journey.delay && journey.delay > 0"
                            color="warning"
                            size="small"
                          >
                            +{{ journey.delay }} min
                          </v-chip>
                          <v-chip
                            v-if="journey.cancelled"
                            color="error"
                            size="small"
                          >
                            Ausfall
                          </v-chip>
                        </v-col>
                      </v-row>
                    </v-expansion-panel-title>

                    <v-expansion-panel-text>
                      <!-- Detaillierte Teilstrecken -->
                      <v-timeline side="end" density="compact">
                        <v-timeline-item
                          v-for="(leg, legIndex) in journey.legs"
                          :key="legIndex"
                          :dot-color="leg.cancelled ? 'error' : 'primary'"
                          size="small"
                        >
                          <template v-slot:opposite>
                            <div class="font-weight-medium">
                              {{ formatTime(leg.departure) }}
                            </div>
                          </template>

                          <v-card>
                            <v-card-text>
                              <div class="d-flex align-center mb-2">
                                <v-icon left color="primary">mdi-train</v-icon>
                                <span class="font-weight-bold mr-2">{{ leg.line || 'Zug' }}</span>
                                <span v-if="leg.direction" class="text-caption">
                                  → {{ leg.direction }}
                                </span>
                              </div>
                              
                              <div class="d-flex justify-space-between">
                                <div>
                                  <div class="text-body-2">{{ leg.from }}</div>
                                  <div class="text-caption text-grey">
                                    Gleis {{ leg.platform || 'N/A' }}
                                  </div>
                                </div>
                                <div class="text-right">
                                  <div class="text-body-2">{{ leg.to }}</div>
                                  <div class="text-caption">
                                    Ankunft: {{ formatTime(leg.arrival) }}
                                  </div>
                                </div>
                              </div>

                              <div v-if="leg.delay && leg.delay > 0" class="mt-2">
                                <v-chip color="warning" size="x-small">
                                  Verspätung: +{{ leg.delay }} min
                                </v-chip>
                              </div>

                              <div v-if="leg.cancelled" class="mt-2">
                                <v-chip color="error" size="x-small">
                                  Ausfall
                                </v-chip>
                              </div>
                            </v-card-text>
                          </v-card>
                        </v-timeline-item>
                      </v-timeline>
                    </v-expansion-panel-text>
                  </v-expansion-panel>
                </v-expansion-panels>
              </div>

              <!-- Keine Ergebnisse -->
              <v-alert
                v-else-if="!loading && connections"
                type="info"
                variant="tonal"
              >
                Keine Verbindungen gefunden
              </v-alert>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </v-container>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { travelService, type TrainConnectionResponse } from '@/services/travelService';

const loading = ref(false);
const error = ref<string | null>(null);
const connections = ref<TrainConnectionResponse | null>(null);

const searchRequest = ref({
  from: 'Laupheim West',
  to: 'Ravensburg',
});

const formatTime = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' });
};

const searchConnections = async () => {
  loading.value = true;
  error.value = null;
  
  try {
    connections.value = await travelService.getConnections({
      from: searchRequest.value.from,
      to: searchRequest.value.to,
    });
  } catch (err: any) {
    error.value = err.response?.data?.error || 'Fehler beim Laden der Verbindungen';
    console.error('Error fetching connections:', err);
  } finally {
    loading.value = false;
  }
};

const loadDefaultConnections = async () => {
  loading.value = true;
  error.value = null;
  
  try {
    connections.value = await travelService.getDefaultConnections();
    searchRequest.value.from = 'Laupheim West';
    searchRequest.value.to = 'Ravensburg';
  } catch (err: any) {
    error.value = err.response?.data?.error || 'Fehler beim Laden der Verbindungen';
    console.error('Error fetching default connections:', err);
  } finally {
    loading.value = false;
  }
};

// Automatisch Standard-Verbindungen laden beim Start
loadDefaultConnections();
</script>

<style scoped>
.travel-view {
  padding: 20px;
}
</style>
