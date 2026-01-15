<template>
  <v-container fluid>
    <!-- Header -->
    <div class="d-flex justify-space-between align-center mb-4 mb-md-6">
      <div class="d-flex align-center">
        <v-btn icon variant="text" @click="$router.back()" class="mr-2 mr-md-3">
          <v-icon>mdi-arrow-left</v-icon>
        </v-btn>
        <h1 :class="isMobile ? 'text-h5' : 'text-h3'">
          <v-icon left color="primary">mdi-brain</v-icon>
          OmniLernen
        </h1>
      </div>

      <!-- Streak Display -->
      <div v-if="streak" class="d-flex align-center">
        <v-chip color="orange" variant="elevated" class="mr-2">
          <v-icon start>mdi-fire</v-icon>
          {{ streak.currentStreak }} Tage
        </v-chip>
        <v-chip v-if="streak.bestStreak > streak.currentStreak" color="amber" variant="outlined" size="small">
          Best: {{ streak.bestStreak }}
        </v-chip>
      </div>
    </div>

    <!-- Quick Stats Row -->
    <v-row class="mb-4">
      <v-col cols="12" sm="6" md="3">
        <v-card color="primary" variant="tonal">
          <v-card-text class="d-flex align-center">
            <v-avatar color="primary" class="mr-3">
              <v-icon>mdi-book-open-variant</v-icon>
            </v-avatar>
            <div>
              <div class="text-h5">{{ stats?.totalEntities || 0 }}</div>
              <div class="text-caption">Wissens-Entitaeten</div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="3">
        <v-card color="success" variant="tonal">
          <v-card-text class="d-flex align-center">
            <v-avatar color="success" class="mr-3">
              <v-icon>mdi-check-circle</v-icon>
            </v-avatar>
            <div>
              <div class="text-h5">{{ stats?.masteredEntities || 0 }}</div>
              <div class="text-caption">Gemeistert</div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="3">
        <v-card color="warning" variant="tonal">
          <v-card-text class="d-flex align-center">
            <v-avatar color="warning" class="mr-3">
              <v-icon>mdi-school</v-icon>
            </v-avatar>
            <div>
              <div class="text-h5">{{ stats?.learningEntities || 0 }}</div>
              <div class="text-caption">In Arbeit</div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="3">
        <v-card color="info" variant="tonal">
          <v-card-text class="d-flex align-center">
            <v-avatar color="info" class="mr-3">
              <v-icon>mdi-percent</v-icon>
            </v-avatar>
            <div>
              <div class="text-h5">{{ Math.round((stats?.overallSuccessRate || 0) * 100) }}%</div>
              <div class="text-caption">Erfolgsrate</div>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <!-- Main Content Tabs -->
    <v-card>
      <v-tabs v-model="activeTab" bg-color="primary" show-arrows>
        <v-tab value="dashboard">
          <v-icon left>mdi-view-dashboard</v-icon>
          Dashboard
        </v-tab>
        <v-tab value="lernen">
          <v-icon left>mdi-play-circle</v-icon>
          Lernen
        </v-tab>
        <v-tab value="entitaeten">
          <v-icon left>mdi-database</v-icon>
          Entitaeten
        </v-tab>
        <v-tab value="graph">
          <v-icon left>mdi-graph</v-icon>
          Wissens-Graph
        </v-tab>
        <v-tab value="fortschritt">
          <v-icon left>mdi-chart-line</v-icon>
          Fortschritt
        </v-tab>
      </v-tabs>

      <v-tabs-window v-model="activeTab">
        <!-- Dashboard Tab -->
        <v-tabs-window-item value="dashboard">
          <v-container fluid>
            <v-row>
              <!-- Prioritaeten -->
              <v-col cols="12" md="6">
                <v-card variant="outlined">
                  <v-card-title class="d-flex align-center">
                    <v-icon left color="error">mdi-alert-circle</v-icon>
                    Prioritaeten
                    <v-spacer />
                    <v-btn size="small" variant="text" @click="loadPriorities" :loading="loadingPriorities">
                      <v-icon>mdi-refresh</v-icon>
                    </v-btn>
                  </v-card-title>
                  <v-card-text>
                    <v-list v-if="priorities.length > 0" density="compact">
                      <v-list-item v-for="(priority, index) in priorities.slice(0, 5)" :key="index" @click="startLearning(priority)">
                        <template #prepend>
                          <v-avatar :color="getPriorityColor(priority.compositeScore)" size="32">
                            {{ index + 1 }}
                          </v-avatar>
                        </template>
                        <v-list-item-title>{{ priority.entityName }}</v-list-item-title>
                        <v-list-item-subtitle>
                          {{ priority.subject }} - {{ priority.topic }}
                          <span v-if="priority.recommendedAction" class="text-primary ml-2">
                            {{ priority.recommendedAction }}
                          </span>
                        </v-list-item-subtitle>
                        <template #append>
                          <v-chip v-if="priority.isBlocked" color="warning" size="small">
                            <v-icon start size="small">mdi-lock</v-icon>
                            Blockiert
                          </v-chip>
                          <v-btn v-else icon size="small" color="primary">
                            <v-icon>mdi-play</v-icon>
                          </v-btn>
                        </template>
                      </v-list-item>
                    </v-list>
                    <v-alert v-else type="info" variant="tonal">
                      Keine Prioritaeten berechnet. Klicke auf Aktualisieren.
                    </v-alert>
                  </v-card-text>
                </v-card>
              </v-col>

              <!-- Schwachstellen -->
              <v-col cols="12" md="6">
                <v-card variant="outlined">
                  <v-card-title class="d-flex align-center">
                    <v-icon left color="warning">mdi-alert</v-icon>
                    Schwachstellen
                  </v-card-title>
                  <v-card-text>
                    <v-list v-if="weakAreas.length > 0" density="compact">
                      <v-list-item v-for="area in weakAreas.slice(0, 5)" :key="area.entityId">
                        <template #prepend>
                          <v-progress-circular :model-value="area.masteryScore * 100" :color="getMasteryColor(area.masteryScore)" size="36" width="3">
                            <span class="text-caption">{{ Math.round(area.masteryScore * 100) }}</span>
                          </v-progress-circular>
                        </template>
                        <v-list-item-title>{{ area.entityName }}</v-list-item-title>
                        <v-list-item-subtitle>
                          {{ area.subject }} - {{ area.topic }}
                        </v-list-item-subtitle>
                        <template #append>
                          <v-btn size="small" color="warning" variant="tonal" @click="practiceWeakArea(area)">
                            Ueben
                          </v-btn>
                        </template>
                      </v-list-item>
                    </v-list>
                    <v-alert v-else type="success" variant="tonal">
                      Keine Schwachstellen gefunden!
                    </v-alert>
                  </v-card-text>
                </v-card>
              </v-col>

              <!-- Schwierigkeitsverteilung -->
              <v-col cols="12" md="6">
                <v-card variant="outlined">
                  <v-card-title class="d-flex align-center">
                    <v-icon left color="info">mdi-chart-pie</v-icon>
                    20/40/40 Verteilung
                  </v-card-title>
                  <v-card-text>
                    <div v-if="distribution" class="mb-4">
                      <div class="d-flex justify-space-between mb-2">
                        <span>Leicht (20%)</span>
                        <span>{{ distribution.easyTotal }} Uebungen ({{ Math.round(distribution.easySuccessRate * 100) }}%)</span>
                      </div>
                      <v-progress-linear :model-value="getDistributionPercent('easy')" color="success" height="20" rounded>
                        <template #default>{{ Math.round(getDistributionPercent('easy')) }}%</template>
                      </v-progress-linear>

                      <div class="d-flex justify-space-between mb-2 mt-3">
                        <span>Mittel (40%)</span>
                        <span>{{ distribution.mediumTotal }} Uebungen ({{ Math.round(distribution.mediumSuccessRate * 100) }}%)</span>
                      </div>
                      <v-progress-linear :model-value="getDistributionPercent('medium')" color="warning" height="20" rounded>
                        <template #default>{{ Math.round(getDistributionPercent('medium')) }}%</template>
                      </v-progress-linear>

                      <div class="d-flex justify-space-between mb-2 mt-3">
                        <span>Schwer (40%)</span>
                        <span>{{ distribution.hardTotal }} Uebungen ({{ Math.round(distribution.hardSuccessRate * 100) }}%)</span>
                      </div>
                      <v-progress-linear :model-value="getDistributionPercent('hard')" color="error" height="20" rounded>
                        <template #default>{{ Math.round(getDistributionPercent('hard')) }}%</template>
                      </v-progress-linear>

                      <v-alert v-if="distribution.distributionAdvice" :type="distribution.followsTwentyFortyForty ? 'success' : 'info'" variant="tonal" class="mt-4">
                        {{ distribution.distributionAdvice }}
                      </v-alert>
                    </div>
                    <v-skeleton-loader v-else type="card" />
                  </v-card-text>
                </v-card>
              </v-col>

              <!-- Bloom Progression -->
              <v-col cols="12" md="6">
                <v-card variant="outlined">
                  <v-card-title class="d-flex align-center">
                    <v-icon left color="purple">mdi-stairs</v-icon>
                    Bloom-Taxonomie Fortschritt
                  </v-card-title>
                  <v-card-text>
                    <div v-if="bloomProgression">
                      <div class="text-center mb-4">
                        <v-chip :color="getBloomLevelColor(bloomProgression.currentLevel)" size="large" class="mr-2">
                          Level {{ bloomProgression.currentLevel }}: {{ bloomProgression.currentLevelName }}
                        </v-chip>
                        <v-icon v-if="bloomProgression.canAdvance" color="success">mdi-arrow-up-bold</v-icon>
                      </div>

                      <v-row dense>
                        <v-col v-for="level in 6" :key="level" cols="4" sm="2">
                          <v-card :color="level <= bloomProgression.currentLevel ? getBloomLevelColor(level) : 'grey-lighten-3'" variant="tonal" class="text-center pa-2">
                            <div class="text-caption">{{ getBloomLevelName(level) }}</div>
                            <v-icon :color="level <= bloomProgression.currentLevel ? 'white' : 'grey'" size="small">
                              {{ level <= bloomProgression.currentLevel ? 'mdi-check' : 'mdi-lock' }}
                            </v-icon>
                          </v-card>
                        </v-col>
                      </v-row>

                      <v-alert v-if="bloomProgression.progressAdvice" :type="bloomProgression.canAdvance ? 'success' : 'info'" variant="tonal" class="mt-4">
                        {{ bloomProgression.progressAdvice }}
                      </v-alert>
                    </div>
                    <v-skeleton-loader v-else type="card" />
                  </v-card-text>
                </v-card>
              </v-col>
            </v-row>
          </v-container>
        </v-tabs-window-item>

        <!-- Lernen Tab -->
        <v-tabs-window-item value="lernen">
          <v-container fluid>
            <v-row>
              <!-- Session Generator -->
              <v-col cols="12" md="4">
                <v-card variant="outlined">
                  <v-card-title>
                    <v-icon left>mdi-play-box-multiple</v-icon>
                    Lern-Session starten
                  </v-card-title>
                  <v-card-text>
                    <v-select v-model="sessionConfig.type" :items="sessionTypes" item-title="text" item-value="value" label="Session-Typ" variant="outlined" density="compact" class="mb-3">
                      <template #item="{ props, item }">
                        <v-list-item v-bind="props">
                          <template #prepend>
                            <v-icon>{{ item.raw.icon }}</v-icon>
                          </template>
                          <v-list-item-subtitle>{{ item.raw.description }}</v-list-item-subtitle>
                        </v-list-item>
                      </template>
                    </v-select>

                    <v-slider v-model="sessionConfig.count" :min="3" :max="20" :step="1" label="Anzahl Uebungen" thumb-label class="mb-3" />

                    <v-select v-model="sessionConfig.subject" :items="availableSubjects" label="Fach (optional)" variant="outlined" density="compact" clearable class="mb-3" />

                    <v-btn color="primary" block size="large" @click="startSession" :loading="loadingSession">
                      <v-icon left>mdi-play</v-icon>
                      Session starten
                    </v-btn>
                  </v-card-text>
                </v-card>

                <!-- Quick Actions -->
                <v-card variant="outlined" class="mt-4">
                  <v-card-title>
                    <v-icon left>mdi-lightning-bolt</v-icon>
                    Schnellaktionen
                  </v-card-title>
                  <v-card-text>
                    <v-btn color="warning" block class="mb-2" @click="practiceOverdue" :loading="loadingOverdue">
                      <v-icon left>mdi-clock-alert</v-icon>
                      Ueberfaellige ({{ overdueItems.length }})
                    </v-btn>
                    <v-btn color="error" block class="mb-2" @click="practiceWeakAreas" :disabled="weakAreas.length === 0">
                      <v-icon left>mdi-alert</v-icon>
                      Schwachstellen ({{ weakAreas.length }})
                    </v-btn>
                    <v-btn color="success" block @click="getNextExercise" :loading="loadingNext">
                      <v-icon left>mdi-arrow-right</v-icon>
                      Naechste Uebung
                    </v-btn>
                  </v-card-text>
                </v-card>
              </v-col>

              <!-- Current Exercise -->
              <v-col cols="12" md="8">
                <v-card v-if="currentExercise" variant="outlined" class="exercise-card">
                  <v-card-title class="d-flex align-center">
                    <v-chip :color="getDifficultyColor(currentExercise.difficulty)" size="small" class="mr-2">
                      {{ getDifficultyLabel(currentExercise.difficulty) }}
                    </v-chip>
                    <v-chip :color="getBloomLevelColor(currentExercise.bloomLevel)" size="small" class="mr-2">
                      {{ currentExercise.bloomLevelName }}
                    </v-chip>
                    <span class="text-body-1">{{ currentExercise.subject }} - {{ currentExercise.topic }}</span>
                    <v-spacer />
                    <v-btn icon size="small" @click="currentExercise = null">
                      <v-icon>mdi-close</v-icon>
                    </v-btn>
                  </v-card-title>
                  <v-divider />
                  <v-card-text>
                    <div class="text-h6 mb-4">{{ currentExercise.question }}</div>

                    <!-- Interactive Exercise Component -->
                    <OmniExercisePlayer v-if="currentExercise" :exercise="currentExercise" @submit="handleSubmitAnswer" @skip="skipExercise" :loading="submittingAnswer" />
                  </v-card-text>
                </v-card>

                <!-- Session Progress -->
                <v-card v-else-if="sessionExercises.length > 0" variant="outlined">
                  <v-card-title>
                    <v-icon left>mdi-format-list-numbered</v-icon>
                    Lern-Session ({{ sessionProgress }}/{{ sessionExercises.length }})
                  </v-card-title>
                  <v-card-text>
                    <v-progress-linear :model-value="(sessionProgress / sessionExercises.length) * 100" color="primary" height="10" rounded class="mb-4" />
                    <v-list>
                      <v-list-item v-for="(ex, index) in sessionExercises" :key="ex.id" :class="{ 'bg-grey-lighten-4': index < sessionProgress }">
                        <template #prepend>
                          <v-icon :color="getSessionItemColor(index)">
                            {{ getSessionItemIcon(index) }}
                          </v-icon>
                        </template>
                        <v-list-item-title>{{ ex.question.substring(0, 60) }}...</v-list-item-title>
                        <v-list-item-subtitle>{{ ex.subject }} - {{ ex.topic }}</v-list-item-subtitle>
                        <template #append>
                          <v-btn v-if="index === sessionProgress" color="primary" size="small" @click="currentExercise = ex">
                            Starten
                          </v-btn>
                        </template>
                      </v-list-item>
                    </v-list>
                  </v-card-text>
                </v-card>

                <!-- Empty State -->
                <v-card v-else variant="outlined" class="text-center pa-8">
                  <v-icon size="100" color="grey-lighten-1">mdi-school-outline</v-icon>
                  <h2 class="text-h5 mt-4 text-grey">Bereit zum Lernen?</h2>
                  <p class="text-grey mt-2">Starte eine Session oder hole dir die naechste Uebung.</p>
                </v-card>
              </v-col>
            </v-row>
          </v-container>
        </v-tabs-window-item>

        <!-- Entitaeten Tab -->
        <v-tabs-window-item value="entitaeten">
          <v-container fluid>
            <!-- Search & Filter -->
            <v-row class="mb-4">
              <v-col cols="12" md="6">
                <v-text-field v-model="entitySearch" label="Suche..." prepend-inner-icon="mdi-magnify" variant="outlined" density="compact" clearable @keyup.enter="searchEntities" />
              </v-col>
              <v-col cols="12" md="3">
                <v-select v-model="entityFilter.subject" :items="availableSubjects" label="Fach" variant="outlined" density="compact" clearable />
              </v-col>
              <v-col cols="12" md="3">
                <v-select v-model="entityFilter.mastery" :items="masteryFilters" item-title="text" item-value="value" label="Meisterschaft" variant="outlined" density="compact" clearable />
              </v-col>
            </v-row>

            <!-- Entity List -->
            <v-card variant="outlined">
              <v-data-table :headers="entityHeaders" :items="filteredEntities" :loading="loadingEntities" :search="entitySearch" items-per-page="15" hover>
                <template #item.masteryScore="{ item }">
                  <v-progress-linear :model-value="item.masteryScore * 100" :color="getMasteryColor(item.masteryScore)" height="20" rounded>
                    <template #default>{{ Math.round(item.masteryScore * 100) }}%</template>
                  </v-progress-linear>
                </template>
                <template #item.currentBloomLevel="{ item }">
                  <v-chip :color="getBloomLevelColor(item.currentBloomLevel)" size="small">
                    {{ getBloomLevelName(item.currentBloomLevel) }}
                  </v-chip>
                </template>
                <template #item.nextReviewDate="{ item }">
                  <span :class="{ 'text-error': isOverdue(item.nextReviewDate) }">
                    {{ getRelativeTime(item.nextReviewDate) }}
                  </span>
                </template>
                <template #item.actions="{ item }">
                  <v-btn icon size="small" color="primary" @click="practiceEntity(item)">
                    <v-icon>mdi-play</v-icon>
                  </v-btn>
                  <v-btn icon size="small" @click="showEntityDetails(item)">
                    <v-icon>mdi-information</v-icon>
                  </v-btn>
                </template>
              </v-data-table>
            </v-card>
          </v-container>
        </v-tabs-window-item>

        <!-- Graph Tab -->
        <v-tabs-window-item value="graph">
          <v-container fluid>
            <v-row>
              <v-col cols="12" md="3">
                <v-card variant="outlined">
                  <v-card-title>
                    <v-icon left>mdi-filter</v-icon>
                    Filter
                  </v-card-title>
                  <v-card-text>
                    <v-select v-model="graphFilter.subject" :items="availableSubjects" label="Fach" variant="outlined" density="compact" clearable class="mb-3" />
                    <v-slider v-model="graphFilter.minStrength" :min="0" :max="1" :step="0.1" label="Min. Staerke" thumb-label class="mb-3" />
                    <v-slider v-model="graphFilter.maxNodes" :min="10" :max="100" :step="10" label="Max. Knoten" thumb-label class="mb-3" />
                    <v-switch v-model="graphFilter.includeWeakEntities" label="Schwache Entitaeten" color="warning" />
                    <v-btn color="primary" block @click="loadGraph" :loading="loadingGraph">
                      Graph laden
                    </v-btn>
                  </v-card-text>
                </v-card>

                <!-- Graph Legend -->
                <v-card variant="outlined" class="mt-4">
                  <v-card-title>
                    <v-icon left>mdi-palette</v-icon>
                    Legende
                  </v-card-title>
                  <v-card-text>
                    <div class="d-flex align-center mb-2">
                      <v-avatar color="#4CAF50" size="24" class="mr-2" />
                      <span>Gemeistert (>80%)</span>
                    </div>
                    <div class="d-flex align-center mb-2">
                      <v-avatar color="#FFC107" size="24" class="mr-2" />
                      <span>In Arbeit (50-80%)</span>
                    </div>
                    <div class="d-flex align-center mb-2">
                      <v-avatar color="#FF9800" size="24" class="mr-2" />
                      <span>Anfaenger (30-50%)</span>
                    </div>
                    <div class="d-flex align-center">
                      <v-avatar color="#F44336" size="24" class="mr-2" />
                      <span>Neu (<30%)</span>
                    </div>
                  </v-card-text>
                </v-card>
              </v-col>

              <v-col cols="12" md="9">
                <v-card variant="outlined" style="height: 600px;">
                  <OmniGraphVisualization v-if="knowledgeGraph" :graph="knowledgeGraph" @node-click="onGraphNodeClick" />
                  <div v-else class="d-flex align-center justify-center" style="height: 100%;">
                    <div class="text-center">
                      <v-icon size="80" color="grey-lighten-1">mdi-graph-outline</v-icon>
                      <p class="text-grey mt-2">Klicke auf "Graph laden" um den Wissens-Graph anzuzeigen.</p>
                    </div>
                  </div>
                </v-card>
              </v-col>
            </v-row>
          </v-container>
        </v-tabs-window-item>

        <!-- Fortschritt Tab -->
        <v-tabs-window-item value="fortschritt">
          <v-container fluid>
            <v-row>
              <!-- Mastery by Subject -->
              <v-col cols="12" md="6">
                <v-card variant="outlined">
                  <v-card-title>
                    <v-icon left>mdi-book-multiple</v-icon>
                    Meisterschaft nach Fach
                  </v-card-title>
                  <v-card-text>
                    <div v-if="stats?.bySubject">
                      <div v-for="(subjectStats, subject) in stats.bySubject" :key="subject" class="mb-4">
                        <div class="d-flex justify-space-between mb-1">
                          <span class="font-weight-medium">{{ subject }}</span>
                          <span>{{ subjectStats.entityCount }} Entitaeten</span>
                        </div>
                        <v-progress-linear :model-value="subjectStats.averageMastery * 100" :color="getMasteryColor(subjectStats.averageMastery)" height="24" rounded>
                          <template #default>
                            {{ Math.round(subjectStats.averageMastery * 100) }}% ({{ Math.round(subjectStats.successRate * 100) }}% Erfolg)
                          </template>
                        </v-progress-linear>
                      </div>
                    </div>
                    <v-skeleton-loader v-else type="list-item@3" />
                  </v-card-text>
                </v-card>
              </v-col>

              <!-- Bloom Level Distribution -->
              <v-col cols="12" md="6">
                <v-card variant="outlined">
                  <v-card-title>
                    <v-icon left>mdi-stairs</v-icon>
                    Bloom-Level Verteilung
                  </v-card-title>
                  <v-card-text>
                    <div v-if="stats?.byBloomLevel">
                      <div v-for="level in 6" :key="level" class="mb-3">
                        <div class="d-flex justify-space-between mb-1">
                          <span>{{ getBloomLevelName(level) }}</span>
                          <span>{{ Math.round((stats.byBloomLevel[level] || 0) * 100) }}%</span>
                        </div>
                        <v-progress-linear :model-value="(stats.byBloomLevel[level] || 0) * 100" :color="getBloomLevelColor(level)" height="16" rounded />
                      </div>
                    </div>
                    <v-skeleton-loader v-else type="list-item@6" />
                  </v-card-text>
                </v-card>
              </v-col>

              <!-- Recent Activity -->
              <v-col cols="12">
                <v-card variant="outlined">
                  <v-card-title>
                    <v-icon left>mdi-calendar</v-icon>
                    Aktivitaet der letzten 30 Tage
                  </v-card-title>
                  <v-card-text>
                    <div v-if="streak?.recentActivityDates" class="d-flex flex-wrap gap-1">
                      <v-tooltip v-for="date in last30Days" :key="date" :text="formatDate(date)">
                        <template #activator="{ props }">
                          <v-avatar v-bind="props" :color="isActivityDay(date) ? 'success' : 'grey-lighten-3'" size="24" class="activity-day">
                            <span class="text-caption">{{ getDayNumber(date) }}</span>
                          </v-avatar>
                        </template>
                      </v-tooltip>
                    </div>
                  </v-card-text>
                </v-card>
              </v-col>
            </v-row>
          </v-container>
        </v-tabs-window-item>
      </v-tabs-window>
    </v-card>

    <!-- Entity Details Dialog -->
    <v-dialog v-model="showEntityDialog" max-width="600">
      <v-card v-if="selectedEntity">
        <v-card-title class="d-flex align-center">
          <v-icon left>mdi-information</v-icon>
          {{ selectedEntity.name }}
          <v-spacer />
          <v-btn icon @click="showEntityDialog = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
        </v-card-title>
        <v-divider />
        <v-card-text>
          <v-list density="compact">
            <v-list-item>
              <template #prepend><v-icon>mdi-book</v-icon></template>
              <v-list-item-title>Fach</v-list-item-title>
              <v-list-item-subtitle>{{ selectedEntity.subject }}</v-list-item-subtitle>
            </v-list-item>
            <v-list-item>
              <template #prepend><v-icon>mdi-tag</v-icon></template>
              <v-list-item-title>Thema</v-list-item-title>
              <v-list-item-subtitle>{{ selectedEntity.topic }}</v-list-item-subtitle>
            </v-list-item>
            <v-list-item>
              <template #prepend><v-icon>mdi-percent</v-icon></template>
              <v-list-item-title>Meisterschaft</v-list-item-title>
              <v-list-item-subtitle>
                <v-progress-linear :model-value="selectedEntity.masteryScore * 100" :color="getMasteryColor(selectedEntity.masteryScore)" height="16" rounded />
              </v-list-item-subtitle>
            </v-list-item>
            <v-list-item>
              <template #prepend><v-icon>mdi-stairs</v-icon></template>
              <v-list-item-title>Bloom-Level</v-list-item-title>
              <v-list-item-subtitle>
                <v-chip :color="getBloomLevelColor(selectedEntity.currentBloomLevel)" size="small">
                  {{ getBloomLevelName(selectedEntity.currentBloomLevel) }}
                </v-chip>
              </v-list-item-subtitle>
            </v-list-item>
            <v-list-item>
              <template #prepend><v-icon>mdi-clock</v-icon></template>
              <v-list-item-title>Naechste Wiederholung</v-list-item-title>
              <v-list-item-subtitle>{{ getRelativeTime(selectedEntity.nextReviewDate) }}</v-list-item-subtitle>
            </v-list-item>
            <v-list-item>
              <template #prepend><v-icon>mdi-counter</v-icon></template>
              <v-list-item-title>Statistik</v-list-item-title>
              <v-list-item-subtitle>{{ selectedEntity.totalCorrect }}/{{ selectedEntity.totalAttempts }} richtig</v-list-item-subtitle>
            </v-list-item>
          </v-list>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn color="primary" @click="practiceEntity(selectedEntity); showEntityDialog = false">
            <v-icon left>mdi-play</v-icon>
            Ueben
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Snackbar -->
    <v-snackbar v-model="snackbar.show" :color="snackbar.color" :timeout="4000">
      {{ snackbar.message }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useDisplay } from 'vuetify'
import api from '@/services/api'
import type {
  MasteryStats, LearningStreak, DifficultyDistribution, BloomProgression,
  Priority, WeakArea, OverdueItem, OmniEntity, OmniExercise, KnowledgeGraph
} from '@/types/omniLearning'
import {
  getMasteryColor, getMasteryLabel, getDifficultyColor, getDifficultyLabel,
  getBloomLevelColor, getBloomLevelName, getRelativeTime, formatDate,
  sessionTypes
} from '@/types/omniLearning'
import OmniExercisePlayer from '@/components/omniLernen/OmniExercisePlayer.vue'
import OmniGraphVisualization from '@/components/omniLernen/OmniGraphVisualization.vue'

const { mobile } = useDisplay()
const isMobile = computed(() => mobile.value)

// State
const activeTab = ref('dashboard')
const loading = ref(false)

// Stats & Analytics
const stats = ref<MasteryStats | null>(null)
const streak = ref<LearningStreak | null>(null)
const distribution = ref<DifficultyDistribution | null>(null)
const bloomProgression = ref<BloomProgression | null>(null)

// Priorities & Weak Areas
const priorities = ref<Priority[]>([])
const weakAreas = ref<WeakArea[]>([])
const overdueItems = ref<OverdueItem[]>([])
const loadingPriorities = ref(false)

// Entities
const entities = ref<OmniEntity[]>([])
const loadingEntities = ref(false)
const entitySearch = ref('')
const entityFilter = ref({ subject: null as string | null, mastery: null as string | null })
const selectedEntity = ref<OmniEntity | null>(null)
const showEntityDialog = ref(false)

// Learning Session
const sessionConfig = ref({ type: 'mixed', count: 5, subject: null as string | null })
const sessionExercises = ref<OmniExercise[]>([])
const sessionProgress = ref(0)
const currentExercise = ref<OmniExercise | null>(null)
const loadingSession = ref(false)
const loadingNext = ref(false)
const loadingOverdue = ref(false)
const submittingAnswer = ref(false)
const sessionResults = ref<{ exerciseId: number; isCorrect: boolean }[]>([])

// Knowledge Graph
const knowledgeGraph = ref<KnowledgeGraph | null>(null)
const loadingGraph = ref(false)
const graphFilter = ref({ subject: null as string | null, minStrength: 0.3, maxNodes: 50, includeWeakEntities: true })

// UI
const snackbar = ref({ show: false, message: '', color: 'success' })

// Computed
const availableSubjects = computed(() => {
  const subjects = new Set(entities.value.map(e => e.subject))
  return Array.from(subjects).sort()
})

const filteredEntities = computed(() => {
  let result = entities.value
  if (entityFilter.value.subject) {
    result = result.filter(e => e.subject === entityFilter.value.subject)
  }
  if (entityFilter.value.mastery) {
    switch (entityFilter.value.mastery) {
      case 'mastered': result = result.filter(e => e.masteryScore >= 0.8); break
      case 'learning': result = result.filter(e => e.masteryScore >= 0.3 && e.masteryScore < 0.8); break
      case 'new': result = result.filter(e => e.masteryScore < 0.3); break
    }
  }
  return result
})

const last30Days = computed(() => {
  const days = []
  for (let i = 29; i >= 0; i--) {
    const date = new Date()
    date.setDate(date.getDate() - i)
    days.push(date.toISOString().split('T')[0])
  }
  return days
})

// Table Headers
const entityHeaders = [
  { title: 'Name', key: 'name', sortable: true },
  { title: 'Fach', key: 'subject', sortable: true },
  { title: 'Thema', key: 'topic', sortable: true },
  { title: 'Meisterschaft', key: 'masteryScore', sortable: true },
  { title: 'Bloom-Level', key: 'currentBloomLevel', sortable: true },
  { title: 'Naechste Wiederholung', key: 'nextReviewDate', sortable: true },
  { title: 'Aktionen', key: 'actions', sortable: false }
]

const masteryFilters = [
  { value: 'mastered', text: 'Gemeistert (>80%)' },
  { value: 'learning', text: 'Lernend (30-80%)' },
  { value: 'new', text: 'Neu (<30%)' }
]

// Methods
const showMessage = (message: string, color = 'success') => {
  snackbar.value = { show: true, message, color }
}

const loadDashboardData = async () => {
  loading.value = true
  try {
    const [statsData, streakData, distData, bloomData] = await Promise.all([
      api.omniGetMasteryStats(),
      api.omniGetStreak(),
      api.omniGetDifficultyDistribution(),
      api.omniGetBloomProgression()
    ])
    stats.value = statsData
    streak.value = streakData
    distribution.value = distData
    bloomProgression.value = bloomData
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Laden der Daten', 'error')
  } finally {
    loading.value = false
  }
}

const loadPriorities = async () => {
  loadingPriorities.value = true
  try {
    priorities.value = await api.omniCalculatePriorities()
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Laden der Prioritaeten', 'error')
  } finally {
    loadingPriorities.value = false
  }
}

const loadWeakAreas = async () => {
  try {
    weakAreas.value = await api.omniGetWeakAreas(10)
  } catch (error: any) {
    console.error('Error loading weak areas:', error)
  }
}

const loadOverdueItems = async () => {
  try {
    overdueItems.value = await api.omniGetOverdueItems()
  } catch (error: any) {
    console.error('Error loading overdue items:', error)
  }
}

const loadEntities = async () => {
  loadingEntities.value = true
  try {
    entities.value = await api.omniGetEntities()
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Laden der Entitaeten', 'error')
  } finally {
    loadingEntities.value = false
  }
}

const searchEntities = async () => {
  if (!entitySearch.value) {
    await loadEntities()
    return
  }
  loadingEntities.value = true
  try {
    entities.value = await api.omniSearchEntities(entitySearch.value)
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler bei der Suche', 'error')
  } finally {
    loadingEntities.value = false
  }
}

const loadGraph = async () => {
  loadingGraph.value = true
  try {
    knowledgeGraph.value = await api.omniGetKnowledgeGraph(graphFilter.value)
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Laden des Graphen', 'error')
  } finally {
    loadingGraph.value = false
  }
}

const startSession = async () => {
  loadingSession.value = true
  try {
    const exercises = await api.omniGenerateSession({
      count: sessionConfig.value.count,
      sessionType: sessionConfig.value.type
    })
    sessionExercises.value = exercises
    sessionProgress.value = 0
    sessionResults.value = []
    if (exercises.length > 0) {
      currentExercise.value = exercises[0]
    }
    showMessage(`Session mit ${exercises.length} Uebungen gestartet!`)
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Starten der Session', 'error')
  } finally {
    loadingSession.value = false
  }
}

const getNextExercise = async () => {
  loadingNext.value = true
  try {
    const exercise = await api.omniGetNextExercise()
    if (exercise) {
      currentExercise.value = exercise
      sessionExercises.value = []
    } else {
      showMessage('Keine faelligen Uebungen vorhanden!', 'info')
    }
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Laden der naechsten Uebung', 'error')
  } finally {
    loadingNext.value = false
  }
}

const handleSubmitAnswer = async (answer: any) => {
  if (!currentExercise.value) return
  submittingAnswer.value = true
  try {
    const startTime = Date.now()
    const result = await api.omniSubmitAnswer(currentExercise.value.id, {
      userAnswer: answer,
      timeTakenMs: Date.now() - startTime
    })

    sessionResults.value.push({ exerciseId: currentExercise.value.id, isCorrect: result.isCorrect })

    if (result.isCorrect) {
      showMessage(`Richtig! Neuer Mastery-Score: ${Math.round(result.newMasteryScore * 100)}%`)
    } else {
      showMessage(result.feedback || 'Leider falsch. Versuch es beim naechsten Mal!', 'warning')
    }

    // Move to next exercise in session
    if (sessionExercises.value.length > 0) {
      sessionProgress.value++
      if (sessionProgress.value < sessionExercises.value.length) {
        currentExercise.value = sessionExercises.value[sessionProgress.value]
      } else {
        // Session complete
        const correct = sessionResults.value.filter(r => r.isCorrect).length
        showMessage(`Session abgeschlossen! ${correct}/${sessionResults.value.length} richtig.`, 'success')
        currentExercise.value = null
        sessionExercises.value = []
        await loadDashboardData()
      }
    } else {
      currentExercise.value = null
      await loadDashboardData()
    }
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Einreichen der Antwort', 'error')
  } finally {
    submittingAnswer.value = false
  }
}

const skipExercise = () => {
  if (sessionExercises.value.length > 0) {
    sessionProgress.value++
    if (sessionProgress.value < sessionExercises.value.length) {
      currentExercise.value = sessionExercises.value[sessionProgress.value]
    } else {
      currentExercise.value = null
      sessionExercises.value = []
    }
  } else {
    currentExercise.value = null
  }
}

const startLearning = (priority: Priority) => {
  if (priority.isBlocked) {
    showMessage(priority.blockReason || 'Diese Entitaet ist blockiert', 'warning')
    return
  }
  practiceEntity({ id: priority.entityId } as OmniEntity)
}

const practiceEntity = async (entity: OmniEntity) => {
  loadingNext.value = true
  try {
    const exercise = await api.omniGenerateExercise({ entityIds: [entity.id] })
    currentExercise.value = exercise
    activeTab.value = 'lernen'
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Generieren der Uebung', 'error')
  } finally {
    loadingNext.value = false
  }
}

const practiceWeakArea = (area: WeakArea) => {
  practiceEntity({ id: area.entityId } as OmniEntity)
}

const practiceWeakAreas = async () => {
  if (weakAreas.value.length === 0) return
  loadingSession.value = true
  try {
    const entityIds = weakAreas.value.slice(0, 5).map(w => w.entityId)
    const exercises = await api.omniGenerateSession({ entityIds, count: 5 })
    sessionExercises.value = exercises
    sessionProgress.value = 0
    if (exercises.length > 0) {
      currentExercise.value = exercises[0]
    }
    activeTab.value = 'lernen'
    showMessage('Schwachstellen-Training gestartet!')
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Starten des Trainings', 'error')
  } finally {
    loadingSession.value = false
  }
}

const practiceOverdue = async () => {
  loadingOverdue.value = true
  try {
    const exercises = await api.omniGetDueExercises(10)
    if (exercises.length === 0) {
      showMessage('Keine ueberfaelligen Uebungen!', 'info')
      return
    }
    sessionExercises.value = exercises
    sessionProgress.value = 0
    currentExercise.value = exercises[0]
    activeTab.value = 'lernen'
    showMessage(`${exercises.length} ueberfaellige Uebungen geladen!`)
  } catch (error: any) {
    showMessage(error.response?.data?.message || 'Fehler beim Laden der ueberfaelligen Uebungen', 'error')
  } finally {
    loadingOverdue.value = false
  }
}

const showEntityDetails = (entity: OmniEntity) => {
  selectedEntity.value = entity
  showEntityDialog.value = true
}

const onGraphNodeClick = (node: any) => {
  const entity = entities.value.find(e => e.id === node.id)
  if (entity) {
    showEntityDetails(entity)
  }
}

// Helper functions
const getPriorityColor = (score: number): string => {
  if (score >= 0.8) return 'error'
  if (score >= 0.5) return 'warning'
  return 'success'
}

const getDistributionPercent = (difficulty: string): number => {
  if (!distribution.value) return 0
  const total = distribution.value.easyTotal + distribution.value.mediumTotal + distribution.value.hardTotal
  if (total === 0) return 0
  switch (difficulty) {
    case 'easy': return (distribution.value.easyTotal / total) * 100
    case 'medium': return (distribution.value.mediumTotal / total) * 100
    case 'hard': return (distribution.value.hardTotal / total) * 100
  }
  return 0
}

const isOverdue = (date: string): boolean => {
  return new Date(date) < new Date()
}

const isActivityDay = (date: string): boolean => {
  return streak.value?.recentActivityDates?.some(d => d.startsWith(date)) || false
}

const getDayNumber = (date: string): number => {
  return new Date(date).getDate()
}

const getSessionItemColor = (index: number): string => {
  if (index < sessionProgress.value) {
    const result = sessionResults.value.find(r => r.exerciseId === sessionExercises.value[index].id)
    return result?.isCorrect ? 'success' : 'error'
  }
  if (index === sessionProgress.value) return 'primary'
  return 'grey'
}

const getSessionItemIcon = (index: number): string => {
  if (index < sessionProgress.value) {
    const result = sessionResults.value.find(r => r.exerciseId === sessionExercises.value[index].id)
    return result?.isCorrect ? 'mdi-check-circle' : 'mdi-close-circle'
  }
  if (index === sessionProgress.value) return 'mdi-play-circle'
  return 'mdi-circle-outline'
}

// Lifecycle
onMounted(async () => {
  await Promise.all([
    loadDashboardData(),
    loadPriorities(),
    loadWeakAreas(),
    loadOverdueItems(),
    loadEntities()
  ])
})

// Watch tab changes to load data
watch(activeTab, async (newTab) => {
  if (newTab === 'graph' && !knowledgeGraph.value) {
    await loadGraph()
  }
})
</script>

<style scoped>
.exercise-card {
  min-height: 400px;
}

.activity-day {
  cursor: default;
}

.gap-1 {
  gap: 4px;
}
</style>
