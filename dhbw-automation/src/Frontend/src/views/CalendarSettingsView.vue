<template>
  <div class="calendar-settings">
    <div class="d-flex align-center mb-6">
      <v-btn icon variant="text" @click="$router.back()" class="mr-3">
        <v-icon>mdi-arrow-left</v-icon>
      </v-btn>
      <h1>Kalender-Einstellungen</h1>
    </div>
    
    <div class="settings-sections">
      <!-- Google Calendar Integration -->
      <section class="settings-section">
        <h2>Google Calendar</h2>
        <p class="section-description">
          Synchronisiere deine DHBW-Termine automatisch mit deinem Google Kalender
        </p>
        
        <GoogleCalendarConnect :user-id="currentUserId" />
      </section>

      <!-- RAPLA Integration -->
      <section class="settings-section">
        <h2>RAPLA Stundenplan</h2>
        <p class="section-description">
          Importiere automatisch deinen Stundenplan aus RAPLA
        </p>
        
        <div class="rapla-config">
          <div class="input-group">
            <label>RAPLA URL</label>
            <input 
              v-model="raplaUrl" 
              type="url" 
              placeholder="https://rapla.dhbw-stuttgart.de/..."
            />
          </div>
          
          <button @click="testRapla" class="btn btn-secondary">
            Verbindung testen
          </button>
          
          <button @click="syncRapla" class="btn btn-primary">
            Jetzt synchronisieren
          </button>
        </div>
      </section>

      <!-- Sync-Einstellungen -->
      <section class="settings-section">
        <h2>Automatische Synchronisation</h2>
        
        <div class="sync-settings">
          <label class="checkbox-label">
            <input type="checkbox" v-model="autoSync" />
            Automatisch synchronisieren
          </label>
          
          <div v-if="autoSync" class="sync-interval">
            <label>Intervall</label>
            <select v-model="syncInterval">
              <option value="5">Alle 5 Minuten</option>
              <option value="15">Alle 15 Minuten</option>
              <option value="30">Alle 30 Minuten</option>
              <option value="60">Stündlich</option>
            </select>
          </div>

          <label class="checkbox-label">
            <input type="checkbox" v-model="bidirectionalSync" />
            Bidirektionale Synchronisation
            <span class="help-text">
              Änderungen in Google Calendar werden zurück synchronisiert
            </span>
          </label>
        </div>
      </section>

      <!-- Benachrichtigungen -->
      <section class="settings-section">
        <h2>Benachrichtigungen</h2>
        
        <div class="notification-settings">
          <label class="checkbox-label">
            <input type="checkbox" v-model="notifyBeforeClass" />
            Vor Vorlesungen benachrichtigen
          </label>
          
          <div v-if="notifyBeforeClass" class="notification-timing">
            <input 
              v-model.number="notifyMinutes" 
              type="number" 
              min="5" 
              max="60"
            />
            <span>Minuten vorher</span>
          </div>

          <label class="checkbox-label">
            <input type="checkbox" v-model="notifyOnSync" />
            Bei Synchronisation benachrichtigen
          </label>
        </div>
      </section>

      <div class="actions">
        <button @click="saveSettings" class="btn btn-primary btn-large">
          Einstellungen speichern
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import GoogleCalendarConnect from '@/components/GoogleCalendarConnect.vue';

const currentUserId = ref(1); // Aus Auth Store holen

// RAPLA
const raplaUrl = ref('');

// Sync Settings
const autoSync = ref(true);
const syncInterval = ref('15');
const bidirectionalSync = ref(true);

// Notifications
const notifyBeforeClass = ref(true);
const notifyMinutes = ref(15);
const notifyOnSync = ref(false);

onMounted(() => {
  loadSettings();
});

const loadSettings = async () => {
  // TODO: Lade Einstellungen vom Backend
};

const saveSettings = async () => {
  // TODO: Speichere Einstellungen im Backend
  alert('Einstellungen gespeichert!');
};

const testRapla = async () => {
  // TODO: Test RAPLA connection
  alert('RAPLA-Verbindung wird getestet...');
};

const syncRapla = async () => {
  // TODO: Sync RAPLA now
  alert('RAPLA wird synchronisiert...');
};
</script>

<style scoped>
.calendar-settings {
  max-width: 800px;
  margin: 0 auto;
  padding: 24px;
}

h1 {
  margin-bottom: 32px;
  color: #1a1a1a;
}

.settings-sections {
  display: flex;
  flex-direction: column;
  gap: 32px;
}

.settings-section {
  background: white;
  border-radius: 12px;
  padding: 24px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.settings-section h2 {
  margin: 0 0 8px;
  font-size: 20px;
  color: #1a1a1a;
}

.section-description {
  color: #666;
  margin: 0 0 24px;
  font-size: 14px;
}

/* RAPLA Config */
.rapla-config {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.input-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.input-group label {
  font-weight: 500;
  color: #1a1a1a;
  font-size: 14px;
}

.input-group input {
  padding: 12px;
  border: 1px solid #dadce0;
  border-radius: 8px;
  font-size: 14px;
}

/* Sync Settings */
.sync-settings,
.notification-settings {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 12px;
  cursor: pointer;
  font-size: 14px;
}

.checkbox-label input[type="checkbox"] {
  width: 20px;
  height: 20px;
  cursor: pointer;
}

.help-text {
  display: block;
  margin-left: 32px;
  color: #666;
  font-size: 12px;
}

.sync-interval,
.notification-timing {
  margin-left: 32px;
  display: flex;
  align-items: center;
  gap: 12px;
}

.sync-interval label {
  font-size: 14px;
  font-weight: 500;
}

.sync-interval select,
.notification-timing input {
  padding: 8px 12px;
  border: 1px solid #dadce0;
  border-radius: 8px;
  font-size: 14px;
}

.notification-timing input {
  width: 80px;
}

/* Buttons */
.btn {
  padding: 12px 24px;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-primary {
  background: #1a73e8;
  color: white;
}

.btn-primary:hover {
  background: #1765cc;
}

.btn-secondary {
  background: white;
  color: #1a73e8;
  border: 1px solid #dadce0;
}

.btn-secondary:hover {
  background: #f8f9fa;
}

.btn-large {
  padding: 16px 32px;
  font-size: 16px;
}

.actions {
  margin-top: 16px;
  text-align: center;
}
</style>
