# DHBW Automation Mobile App

Flutter-basierte iOS/Android-App für das DHBW Study Automation System.

## Features

✅ **Implementiert:**
- 🔐 Login & Registrierung
- 📅 Kalender mit Rapla-Sync (Auto-Refresh alle 30s + Pull-to-Refresh)
- ✅ Todo-Listen mit CRUD-Operationen
- 📧 Email-Inbox mit Actions (Accept/Decline/Snooze)
- 📸 File-Upload (Kamera & Galerie)
- 🔄 Automatisches Polling (alle 30s)
- 📱 Bottom Navigation
- 🎨 Material Design 3

## Voraussetzungen

- Flutter SDK 3.2.0+
- Dart SDK 3.2.0+
- Android Studio / Xcode
- Backend läuft auf `http://localhost:5000`

## Installation

### 1. Dependencies installieren

```bash
cd mobile
flutter pub get
```

### 2. Backend-URL konfigurieren

Bearbeite `lib/core/config/api_config.dart`:

```dart
static const String baseUrl = 'http://DEINE-IP:5000/api';  // Für Android-Emulator: 10.0.2.2
```

### 3. Android-Emulator

```bash
flutter run
```

### 4. iOS-Simulator (macOS)

```bash
flutter run
```

### 5. Auf echtem Gerät

**Android:**
- USB-Debugging aktivieren
- `flutter run`

**iOS:**
- Apple Developer Account erforderlich
- Gerät in Xcode registrieren
- `flutter run`

## API-Konfiguration

**Standard:** `http://localhost:5000/api`

**Android-Emulator:** `http://10.0.2.2:5000/api`

**Echtes Gerät:** `http://192.168.x.x:5000/api` (Backend-IP im lokalen Netzwerk)

## Projekt-Struktur

```
mobile/
├── lib/
│   ├── core/
│   │   ├── config/         # API-Konfiguration
│   │   ├── models/         # Datenmodelle (User, Todo, Email, etc.)
│   │   └── services/       # API-Services (Auth, Calendar, Todo, Mail, File)
│   ├── providers/          # State Management (Provider)
│   ├── screens/            # UI-Screens
│   │   ├── auth/           # Login, Register
│   │   ├── calendar/       # Kalender-Ansicht
│   │   ├── todo/           # Todo-Listen
│   │   ├── mail/           # Email-Inbox
│   │   └── profile/        # Profil & Einstellungen
│   ├── widgets/            # Wiederverwendbare Widgets
│   └── main.dart           # App-Entry
├── android/                # Android-Konfiguration
├── ios/                    # iOS-Konfiguration
└── pubspec.yaml            # Dependencies
```

## Features im Detail

### Kalender
- Wochenansicht mit Events
- Rapla-Synchronisation
- Auto-Refresh alle 30s
- Pull-to-Refresh
- Farbkodierung nach Quelle (Rapla/Google/Manual)

### Todos
- Erstellen mit Titel, Beschreibung, Fälligkeitsdatum
- Kategorien & Prioritäten
- Filter: Alle / Aktiv / Erledigt
- Swipe-to-Delete
- Checkbox zum Abhaken

### Emails
- Inbox mit Ungelesen-Badge
- Filter: Alle / Ungelesen / Aktion erforderlich
- Email-Actions: Accept, Decline, Snooze
- Prioritätskennzeichnung

### File-Upload
- Foto von Kamera aufnehmen
- Aus Galerie wählen
- Kategorie-Auswahl (Vorlesung, Aufgabe, Notizen, etc.)
- Upload-Progress-Anzeige
- Automatische Backend-Analyse (AI)

## Nächste Schritte (Optional)

### Push-Notifications (FCM)
1. Firebase-Projekt erstellen
2. `google-services.json` (Android) und `GoogleService-Info.plist` (iOS) hinzufügen
3. Backend: FCM-Integration für Notifications

### Google Calendar OAuth
1. Google Cloud Console: OAuth-Credentials
2. WebView für OAuth-Flow
3. Deep-Linking für Callback

### Learning-Modul
- Fällige Übungen anzeigen
- Spaced Repetition
- Fortschritts-Tracking

## Troubleshooting

**"Connection refused":**
- Backend läuft nicht
- Falsche IP/Port in `api_config.dart`

**"Camera not available":**
- Permissions in `AndroidManifest.xml` / `Info.plist` prüfen
- App-Berechtigungen in Geräte-Einstellungen

**Build-Fehler:**
```bash
flutter clean
flutter pub get
flutter run
```

## Test-Credentials

```
Email: student@dhbw.de
Password: test123
```

## Entwicklung

**Hot-Reload während Entwicklung:**
```bash
flutter run
# Drücke 'r' für Hot Reload
# Drücke 'R' für Hot Restart
```

**Logging aktivieren:**
```dart
// In api_client.dart ist PrettyDioLogger bereits aktiviert
// Logs in Terminal/Console ansehen
```

## Deployment

### Android (APK)
```bash
flutter build apk --release
# APK: build/app/outputs/flutter-apk/app-release.apk
```

### iOS (IPA)
```bash
flutter build ios --release
# In Xcode öffnen und signieren
```

## Support

Bei Fragen: README im Hauptprojekt konsultieren oder Issues erstellen.

---

**Version:** 1.0.0  
**Framework:** Flutter 3.2+  
**Backend:** .NET 8 Web API
