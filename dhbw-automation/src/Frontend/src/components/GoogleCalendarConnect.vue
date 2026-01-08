<template>
  <div class="google-calendar-connect">
    <!-- Nicht verbunden -->
    <div v-if="!isConnected && !loading" class="connect-card">
      <div class="icon">📅</div>
      <h3>Google Kalender verbinden</h3>
      <p>Verbinde deinen Google Kalender, um automatisch alle Termine zu synchronisieren</p>
      
      <button @click="connectGoogle" class="google-btn">
        <svg class="google-icon" viewBox="0 0 24 24">
          <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
          <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
          <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
          <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
        </svg>
        Mit Google anmelden
      </button>

      <div class="info">
        <small>
          ✓ Automatische Synchronisation<br>
          ✓ Bidirektionale Updates<br>
          ✓ Sichere OAuth 2.0 Authentifizierung
        </small>
      </div>
    </div>

    <!-- Verbunden -->
    <div v-else-if="isConnected && !loading" class="connected-card">
      <div class="status-header">
        <div class="status-icon">✓</div>
        <div>
          <h4>Google Kalender verbunden</h4>
          <p class="email">{{ userEmail }}</p>
        </div>
      </div>

      <div class="sync-info">
        <div v-if="lastSync" class="last-sync">
          Letzte Synchronisation: {{ formatDate(lastSync) }}
        </div>
        <div v-if="syncStats" class="stats">
          <span>{{ syncStats.imported }} importiert</span>
          <span>{{ syncStats.exported }} exportiert</span>
        </div>
      </div>

      <div class="actions">
        <button @click="syncNow" :disabled="syncing" class="sync-btn">
          <span v-if="!syncing">🔄 Jetzt synchronisieren</span>
          <span v-else>⏳ Synchronisiere...</span>
        </button>
        
        <button @click="disconnect" class="disconnect-btn">
          Trennen
        </button>
      </div>
    </div>

    <!-- Laden -->
    <div v-else class="loading">
      <div class="spinner"></div>
      <p>Verbinde mit Google...</p>
    </div>

    <!-- Fehler -->
    <div v-if="error" class="error-message">
      {{ error }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';

interface Props {
  userId: number;
}

const props = defineProps<Props>();
const router = useRouter();
const route = useRoute();

const isConnected = ref(false);
const loading = ref(true);
const syncing = ref(false);
const error = ref('');
const userEmail = ref('');
const lastSync = ref<Date | null>(null);
const syncStats = ref<{ imported: number; exported: number } | null>(null);

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

onMounted(async () => {
  // Prüfe ob Callback von Google
  if (route.query.googleConnected === 'true') {
    await checkConnection();
    router.replace({ query: {} }); // Query Parameter entfernen
  } else {
    await checkConnection();
  }
});

const checkConnection = async () => {
  loading.value = true;
  error.value = '';
  
  try {
    const response = await fetch(`${API_BASE}/api/calendar/google/status/${props.userId}`);
    const data = await response.json();
    
    if (data.success) {
      isConnected.value = data.data.isConnected;
      if (isConnected.value) {
        // Optional: Email vom Backend holen
        userEmail.value = 'Google Account'; // Könnte aus User-Profil kommen
      }
    }
  } catch (err) {
    error.value = 'Fehler beim Prüfen der Verbindung';
    console.error(err);
  } finally {
    loading.value = false;
  }
};

const connectGoogle = async () => {
  loading.value = true;
  error.value = '';
  
  try {
    const response = await fetch(`${API_BASE}/api/calendar/google/authorize/${props.userId}`);
    const data = await response.json();
    
    if (data.success && data.data.authorizationUrl) {
      // Öffne Google OAuth in gleichem Fenster
      window.location.href = data.data.authorizationUrl;
    } else {
      error.value = 'Fehler beim Starten der Autorisierung';
    }
  } catch (err) {
    error.value = 'Verbindungsfehler';
    console.error(err);
    loading.value = false;
  }
};

const syncNow = async () => {
  syncing.value = true;
  error.value = '';
  
  try {
    const response = await fetch(
      `${API_BASE}/api/calendar/google/sync-bidirectional/${props.userId}`,
      { method: 'POST' }
    );
    const data = await response.json();
    
    if (data.success) {
      syncStats.value = {
        imported: data.data.importedEvents,
        exported: data.data.exportedEvents
      };
      lastSync.value = new Date();
    } else {
      error.value = 'Synchronisation fehlgeschlagen';
    }
  } catch (err) {
    error.value = 'Fehler bei der Synchronisation';
    console.error(err);
  } finally {
    syncing.value = false;
  }
};

const disconnect = async () => {
  if (!confirm('Google Kalender Verbindung wirklich trennen?')) {
    return;
  }
  
  // TODO: Implement disconnect endpoint
  // Für jetzt: Token lokal löschen lassen
  isConnected.value = false;
  userEmail.value = '';
  lastSync.value = null;
  syncStats.value = null;
};

const formatDate = (date: Date) => {
  return new Intl.DateTimeFormat('de-DE', {
    dateStyle: 'short',
    timeStyle: 'short'
  }).format(date);
};
</script>

<style scoped>
.google-calendar-connect {
  max-width: 500px;
  margin: 0 auto;
}

.connect-card,
.connected-card {
  background: white;
  border-radius: 12px;
  padding: 32px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  text-align: center;
}

.icon {
  font-size: 64px;
  margin-bottom: 16px;
}

h3 {
  margin: 0 0 8px;
  color: #1a1a1a;
}

p {
  color: #666;
  margin: 0 0 24px;
  line-height: 1.5;
}

.google-btn {
  display: inline-flex;
  align-items: center;
  gap: 12px;
  padding: 12px 32px;
  background: white;
  border: 1px solid #dadce0;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 500;
  color: #3c4043;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.google-btn:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  border-color: #bdc1c6;
}

.google-icon {
  width: 20px;
  height: 20px;
}

.info {
  margin-top: 24px;
  padding-top: 24px;
  border-top: 1px solid #eee;
}

.info small {
  color: #666;
  line-height: 1.8;
}

/* Connected State */
.connected-card {
  text-align: left;
}

.status-header {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 24px;
}

.status-icon {
  width: 48px;
  height: 48px;
  background: #34a853;
  color: white;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.status-header h4 {
  margin: 0;
  color: #1a1a1a;
}

.email {
  color: #666;
  font-size: 14px;
  margin: 4px 0 0;
}

.sync-info {
  background: #f8f9fa;
  padding: 16px;
  border-radius: 8px;
  margin-bottom: 16px;
}

.last-sync {
  font-size: 14px;
  color: #666;
  margin-bottom: 8px;
}

.stats {
  display: flex;
  gap: 16px;
  font-size: 14px;
}

.stats span {
  color: #34a853;
  font-weight: 500;
}

.actions {
  display: flex;
  gap: 12px;
}

.sync-btn {
  flex: 1;
  padding: 12px 24px;
  background: #1a73e8;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s;
}

.sync-btn:hover:not(:disabled) {
  background: #1765cc;
}

.sync-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.disconnect-btn {
  padding: 12px 24px;
  background: white;
  color: #d93025;
  border: 1px solid #dadce0;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.disconnect-btn:hover {
  background: #fef7f7;
  border-color: #d93025;
}

/* Loading */
.loading {
  text-align: center;
  padding: 48px;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #1a73e8;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 16px;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

/* Error */
.error-message {
  margin-top: 16px;
  padding: 12px;
  background: #fef7f7;
  border: 1px solid #f5c6cb;
  border-radius: 8px;
  color: #d93025;
  font-size: 14px;
}
</style>
