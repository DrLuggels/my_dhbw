# Google Calendar Integration - Setup Anleitung

## 1. Google Cloud Projekt einrichten

### OAuth 2.0 Credentials erstellen

1. Gehe zu [Google Cloud Console](https://console.cloud.google.com/)
2. Erstelle ein neues Projekt oder wähle ein bestehendes aus
3. **Google Calendar API aktivieren:**
   - Navigation → APIs & Services → Library
   - Suche nach "Google Calendar API"
   - Klicke auf "Enable"

4. **OAuth 2.0 Credentials erstellen:**
   - Navigation → APIs & Services → Credentials
   - Klicke auf "+ CREATE CREDENTIALS" → "OAuth 2.0 Client ID"
   - Falls noch nicht konfiguriert: Konfiguriere den OAuth consent screen
     - User Type: External (für Tests) oder Internal (für Unternehmens-Workspace)
     - App Name: "DHBW Automation"
     - User support email: Deine E-Mail
     - Scopes: Keine zusätzlichen Scopes nötig (werden später hinzugefügt)
   - Application type: **Web application**
   - Name: "DHBW Calendar Sync"
   - Authorized redirect URIs:
     - Für lokale Entwicklung: `http://localhost:5000/api/calendar/google/callback`
     - Für Produktion: `https://deine-domain.com/api/calendar/google/callback`
   - Klicke auf "CREATE"

5. **Credentials speichern:**
   - Nach dem Erstellen wird ein Dialog mit Client ID und Client Secret angezeigt
   - **WICHTIG:** Kopiere beide Werte sofort!

## 2. Backend Konfiguration

### NuGet Pakete installieren

```bash
cd dhbw-automation/src/Backend_New/DHBWAutomation.Infrastructure
dotnet add package Google.Apis.Calendar.v3
dotnet add package Google.Apis.Auth
```

### appsettings.json aktualisieren

Öffne `appsettings.json` und füge die Google Credentials ein:

```json
{
  "Google": {
    "ClientId": "DEINE_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "DEIN_CLIENT_SECRET",
    "RedirectUri": "http://localhost:5000/api/calendar/google/callback"
  }
}
```

**WICHTIG:** Füge `appsettings.json` zu `.gitignore` hinzu oder verwende User Secrets:

```bash
cd dhbw-automation/src/Backend_New/DHBWAutomation.API
dotnet user-secrets set "Google:ClientId" "DEINE_CLIENT_ID"
dotnet user-secrets set "Google:ClientSecret" "DEIN_CLIENT_SECRET"
```

### Service registrieren

In `Program.cs` (Backend):

```csharp
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Services;

// ...

// Google Calendar Service registrieren
builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();
```

## 3. Verwendung

### Workflow für Benutzer

1. **Mit Google verbinden:**
   ```
   GET /api/calendar/google/authorize/{userId}
   ```
   - Gibt eine Authorization URL zurück
   - Benutzer öffnet die URL im Browser
   - Meldet sich mit Google an und gibt Zugriff auf den Kalender
   - Wird zu `/api/calendar/google/callback` weitergeleitet

2. **Verbindungsstatus prüfen:**
   ```
   GET /api/calendar/google/status/{userId}
   ```

3. **Events von Google importieren:**
   ```
   POST /api/calendar/google/sync-from/{userId}?startDate=2026-01-01&endDate=2026-12-31
   ```

4. **Events zu Google exportieren:**
   ```
   POST /api/calendar/google/sync-to/{userId}
   ```

5. **Bidirektionale Synchronisation:**
   ```
   POST /api/calendar/google/sync-bidirectional/{userId}
   ```

### API Endpoints

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| GET | `/api/calendar/google/authorize/{userId}` | Startet OAuth Autorisierung |
| GET | `/api/calendar/google/callback` | OAuth Callback (automatisch) |
| GET | `/api/calendar/google/status/{userId}` | Prüft Verbindungsstatus |
| POST | `/api/calendar/google/sync-from/{userId}` | Importiert von Google |
| POST | `/api/calendar/google/sync-to/{userId}` | Exportiert zu Google |
| POST | `/api/calendar/google/sync-bidirectional/{userId}` | Bidirektionale Sync |

### Beispiel: Vollständiger Workflow

```javascript
// 1. Autorisierung starten
const authResponse = await fetch('/api/calendar/google/authorize/1');
const { data } = await authResponse.json();
window.location.href = data.authorizationUrl;

// 2. Nach Redirect: Verbindung prüfen
const statusResponse = await fetch('/api/calendar/google/status/1');
const status = await statusResponse.json();
console.log('Verbunden:', status.data.isConnected);

// 3. Events synchronisieren
const syncResponse = await fetch('/api/calendar/google/sync-bidirectional/1', {
  method: 'POST'
});
const syncResult = await syncResponse.json();
console.log(`${syncResult.data.importedEvents} importiert, ${syncResult.data.exportedEvents} exportiert`);
```

## 4. Frontend Integration

### Vue.js Beispiel

Erstelle einen Composable für Google Calendar:

```typescript
// src/Frontend/src/composables/useGoogleCalendar.ts
import { ref } from 'vue';
import { apiClient } from '@/services/api';

export function useGoogleCalendar() {
  const isConnected = ref(false);
  const isLoading = ref(false);

  const checkConnection = async (userId: number) => {
    try {
      const response = await apiClient.get(`/calendar/google/status/${userId}`);
      isConnected.value = response.data.data.isConnected;
      return isConnected.value;
    } catch (error) {
      console.error('Fehler beim Prüfen der Verbindung:', error);
      return false;
    }
  };

  const authorize = async (userId: number) => {
    try {
      const response = await apiClient.get(`/calendar/google/authorize/${userId}`);
      window.location.href = response.data.data.authorizationUrl;
    } catch (error) {
      console.error('Fehler bei der Autorisierung:', error);
    }
  };

  const syncFromGoogle = async (userId: number) => {
    isLoading.value = true;
    try {
      const response = await apiClient.post(`/calendar/google/sync-from/${userId}`);
      return response.data.data.syncedEvents;
    } catch (error) {
      console.error('Fehler beim Import:', error);
      throw error;
    } finally {
      isLoading.value = false;
    }
  };

  const syncToGoogle = async (userId: number) => {
    isLoading.value = true;
    try {
      const response = await apiClient.post(`/calendar/google/sync-to/${userId}`);
      return response.data.data.exportedEvents;
    } catch (error) {
      console.error('Fehler beim Export:', error);
      throw error;
    } finally {
      isLoading.value = false;
    }
  };

  const syncBidirectional = async (userId: number) => {
    isLoading.value = true;
    try {
      const response = await apiClient.post(`/calendar/google/sync-bidirectional/${userId}`);
      return {
        imported: response.data.data.importedEvents,
        exported: response.data.data.exportedEvents
      };
    } catch (error) {
      console.error('Fehler bei der Synchronisation:', error);
      throw error;
    } finally {
      isLoading.value = false;
    }
  };

  return {
    isConnected,
    isLoading,
    checkConnection,
    authorize,
    syncFromGoogle,
    syncToGoogle,
    syncBidirectional
  };
}
```

### Kalender-View mit Google Sync Button

```vue
<template>
  <div class="calendar-view">
    <div class="sync-controls">
      <div v-if="!googleConnected">
        <button @click="connectGoogle" class="btn btn-primary">
          📅 Mit Google Calendar verbinden
        </button>
      </div>
      
      <div v-else class="connected-controls">
        <span class="status">✓ Google Calendar verbunden</span>
        <button @click="sync" :disabled="syncing" class="btn btn-success">
          {{ syncing ? 'Synchronisiere...' : '🔄 Jetzt synchronisieren' }}
        </button>
      </div>
    </div>

    <!-- Kalender-Komponente -->
    <CalendarComponent :events="events" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useGoogleCalendar } from '@/composables/useGoogleCalendar';

const userId = ref(1); // Aus Auth Store holen
const googleConnected = ref(false);
const syncing = ref(false);
const events = ref([]);

const { checkConnection, authorize, syncBidirectional } = useGoogleCalendar();

onMounted(async () => {
  googleConnected.value = await checkConnection(userId.value);
});

const connectGoogle = async () => {
  await authorize(userId.value);
};

const sync = async () => {
  syncing.value = true;
  try {
    const result = await syncBidirectional(userId.value);
    alert(`Sync erfolgreich! ${result.imported} importiert, ${result.exported} exportiert`);
    // Events neu laden
    await loadEvents();
  } catch (error) {
    alert('Fehler bei der Synchronisation');
  } finally {
    syncing.value = false;
  }
};
</script>
```

## 5. Automatische Synchronisation

### Cron Job (optional)

Für automatische Synchronisation alle 15 Minuten:

```csharp
// In Program.cs
builder.Services.AddHostedService<GoogleCalendarSyncBackgroundService>();

// Neuer Service: GoogleCalendarSyncBackgroundService.cs
public class GoogleCalendarSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GoogleCalendarSyncBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    public GoogleCalendarSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<GoogleCalendarSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var googleService = scope.ServiceProvider.GetRequiredService<IGoogleCalendarService>();

                var users = await context.Users.ToListAsync(stoppingToken);

                foreach (var user in users)
                {
                    if (await googleService.IsConnectedAsync(user.Id))
                    {
                        await googleService.SyncBidirectionalAsync(user.Id);
                        _logger.LogInformation($"Auto-Sync für User {user.Id} abgeschlossen");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei automatischer Google Calendar Sync");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
```

## 6. Troubleshooting

### Häufige Probleme

1. **"Google Calendar Service ist nicht konfiguriert"**
   - Prüfe ob `IGoogleCalendarService` in `Program.cs` registriert ist
   - Prüfe ob NuGet-Pakete installiert sind

2. **OAuth-Fehler "redirect_uri_mismatch"**
   - Redirect URI in Google Cloud Console muss EXAKT übereinstimmen
   - Achte auf http vs. https
   - Achte auf Port-Nummern

3. **"401 Unauthorized" bei API-Calls**
   - Token ist abgelaufen → Benutzer muss sich neu autorisieren
   - Lösche `GoogleCalendarTokens` Ordner und autorisiere neu

4. **Events werden nicht synchronisiert**
   - Prüfe `ExternalId` Feld in der Datenbank
   - Prüfe Datumsbereiche bei Sync-Calls
   - Aktiviere Debug-Logging

### Logs aktivieren

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DHBWAutomation.Backend.Core.Services.GoogleCalendarService": "Debug"
    }
  }
}
```

## 7. Sicherheit

- **User Secrets verwenden** statt Hardcoding in appsettings.json
- **HTTPS verwenden** in Produktion
- **Scopes minimieren** - nur Calendar-Zugriff anfragen
- **Tokens sicher speichern** - der FileDataStore speichert Tokens lokal
- Für Produktion: Implementiere Token-Encryption

## Support

Bei Problemen:
- Google Calendar API Dokumentation: https://developers.google.com/calendar/api
- OAuth 2.0 Guide: https://developers.google.com/identity/protocols/oauth2
