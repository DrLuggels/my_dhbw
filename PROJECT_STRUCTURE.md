# 📁 DHBW Study Automation System - Projekt-Struktur

Detaillierte Dokumentation der Projekt-Ordnerstruktur und Architektur-Entscheidungen.

## 🏗️ Überblick

```
dhbw-automation/
├── src/                        # Quellcode
├── tests/                      # Tests
├── docs/                       # Dokumentation
├── database/                   # Datenbank-Skripte
├── docker/                     # Docker-Konfigurationen
├── .github/                    # GitHub Actions CI/CD
└── ...                         # Config-Dateien
```

---

## 📂 Root-Ebene

```
dhbw-automation/
├── .env                        # Environment Variables (NICHT committen!)
├── .env.example                # Template für .env
├── .gitignore                  # Git Ignore Rules
├── README.md                   # Projekt-Übersicht
├── CONTRIBUTING.md             # Contribution Guidelines
├── SETUP_GUIDE.md              # Setup-Anleitung
├── PROJECT_STRUCTURE.md        # Diese Datei
├── CHANGELOG.md                # Versions-Historie
├── LICENSE                     # MIT Lizenz
├── docker-compose.yml          # Development Environment
├── docker-compose.prod.yml     # Production Environment
└── .editorconfig               # Editor-Konfiguration
```

### Wichtige Root-Dateien

#### `.env`
Enthält alle sensiblen Daten und Konfigurationen. **NIEMALS committen!**

#### `docker-compose.yml`
Definiert alle Services für lokale Entwicklung:
- MariaDB
- Redis
- MinIO
- RabbitMQ
- Qdrant

---

## 🔧 Backend (`src/Backend/`)

```
src/Backend/
├── API/                        # REST API Layer
│   ├── Controllers/            # API Endpoints
│   │   ├── AuthController.cs
│   │   ├── FilesController.cs
│   │   ├── CalendarController.cs
│   │   ├── MailController.cs
│   │   ├── MoodleController.cs
│   │   ├── LectureController.cs
│   │   └── TutorController.cs
│   │
│   ├── Middleware/             # Request/Response Pipeline
│   │   ├── AuthenticationMiddleware.cs
│   │   ├── ExceptionMiddleware.cs
│   │   ├── RateLimitMiddleware.cs
│   │   └── LoggingMiddleware.cs
│   │
│   ├── Filters/                # Action Filters
│   │   ├── ValidateModelAttribute.cs
│   │   └── AuthorizeRoleAttribute.cs
│   │
│   ├── Program.cs              # Application Entry Point
│   └── appsettings.json        # Configuration
│
├── Core/                       # Business Logic Layer
│   ├── Services/               # Business Services
│   │   ├── FileService.cs
│   │   ├── AIService.cs
│   │   ├── CalendarService.cs
│   │   ├── MailService.cs
│   │   ├── MoodleService.cs
│   │   ├── NotificationService.cs
│   │   ├── LectureLiveService.cs
│   │   └── TutorService.cs
│   │
│   ├── Interfaces/             # Service Contracts
│   │   ├── IFileService.cs
│   │   ├── IAIService.cs
│   │   └── ...
│   │
│   ├── Models/                 # Domain Models
│   │   ├── User.cs
│   │   ├── Document.cs
│   │   ├── Course.cs
│   │   ├── Event.cs
│   │   ├── Reminder.cs
│   │   └── LectureSession.cs
│   │
│   └── DTOs/                   # Data Transfer Objects
│       ├── Requests/
│       │   ├── LoginRequest.cs
│       │   ├── UploadFileRequest.cs
│       │   └── CreateReminderRequest.cs
│       │
│       └── Responses/
│           ├── UserResponse.cs
│           ├── DocumentResponse.cs
│           └── CalendarEventResponse.cs
│
├── Infrastructure/             # External Integrations
│   ├── Database/               # Database Context
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/     # Entity Configurations
│   │   │   ├── UserConfiguration.cs
│   │   │   ├── DocumentConfiguration.cs
│   │   │   └── ...
│   │   │
│   │   └── Migrations/         # EF Migrations
│   │       └── ...
│   │
│   ├── Repositories/           # Data Access Layer
│   │   ├── UserRepository.cs
│   │   ├── DocumentRepository.cs
│   │   └── GenericRepository.cs
│   │
│   ├── ExternalAPIs/           # Third-Party API Clients
│   │   ├── OpenAI/
│   │   │   ├── OpenAIClient.cs
│   │   │   └── WhisperClient.cs
│   │   │
│   │   ├── Anthropic/
│   │   │   └── ClaudeClient.cs
│   │   │
│   │   ├── Google/
│   │   │   ├── GmailClient.cs
│   │   │   └── CalendarClient.cs
│   │   │
│   │   ├── Moodle/
│   │   │   └── MoodleApiClient.cs
│   │   │
│   │   └── Deepgram/
│   │       └── DeepgramClient.cs
│   │
│   ├── Storage/                # File Storage
│   │   ├── MinIOService.cs
│   │   └── LocalStorageService.cs
│   │
│   └── Cache/                  # Caching
│       └── RedisCacheService.cs
│
├── BackgroundWorkers/          # Background Services
│   ├── MailPollerWorker.cs     # Polls mail every 5 min
│   ├── MoodleSyncWorker.cs     # Polls Moodle every 1 min
│   ├── FileProcessorWorker.cs  # Processes uploaded files
│   ├── NotificationWorker.cs   # Sends scheduled notifications
│   ├── BackupWorker.cs         # Daily backup
│   └── ReminderWorker.cs       # Checks for due reminders
│
├── Shared/                     # Shared Code
│   ├── Constants/
│   │   └── AppConstants.cs
│   │
│   ├── Extensions/
│   │   ├── StringExtensions.cs
│   │   └── DateTimeExtensions.cs
│   │
│   ├── Helpers/
│   │   ├── EncryptionHelper.cs
│   │   ├── FileHelper.cs
│   │   └── PdfHelper.cs
│   │
│   └── Utilities/
│       ├── Logger.cs
│       └── EmailValidator.cs
│
└── Backend.csproj              # Project File
```

### Backend Architektur-Prinzipien

#### 1. **Layered Architecture**

```
┌─────────────────┐
│   API Layer    │ ← Controllers, Middleware
├─────────────────┤
│   Core Layer   │ ← Business Logic, Services
├─────────────────┤
│ Infrastructure │ ← Database, External APIs
└─────────────────┘
```

#### 2. **Dependency Injection**

Alle Services werden via DI Container registered:

```csharp
// Program.cs
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddSingleton<RedisCacheService>();
```

#### 3. **Repository Pattern**

Data Access wird über Repositories abstrahiert:

```csharp
public interface IDocumentRepository
{
    Task<Document> GetByIdAsync(int id);
    Task<IEnumerable<Document>> GetAllAsync();
    Task AddAsync(Document document);
    Task UpdateAsync(Document document);
    Task DeleteAsync(int id);
}
```

#### 4. **Service Layer**

Business Logic in Services:

```csharp
public class FileService : IFileService
{
    private readonly IDocumentRepository _repository;
    private readonly IAIService _aiService;
    private readonly IMinIOService _storage;
    
    public async Task<DocumentResponse> ProcessUploadAsync(
        IFormFile file, 
        int userId
    )
    {
        // 1. Validate file
        // 2. Upload to MinIO
        // 3. AI Analysis
        // 4. Extract metadata
        // 5. Save to database
        // 6. Return response
    }
}
```

---

## 🎨 Frontend (`src/Frontend/`)

```
src/Frontend/
├── public/                     # Static Assets
│   ├── favicon.ico
│   └── robots.txt
│
├── src/
│   ├── main.ts                 # Application Entry
│   ├── App.vue                 # Root Component
│   │
│   ├── assets/                 # Images, Fonts, etc.
│   │   ├── images/
│   │   ├── icons/
│   │   └── styles/
│   │       ├── main.scss
│   │       ├── variables.scss
│   │       └── mixins.scss
│   │
│   ├── components/             # Reusable Components
│   │   ├── common/             # Generic Components
│   │   │   ├── Button.vue
│   │   │   ├── Card.vue
│   │   │   ├── Modal.vue
│   │   │   ├── Loader.vue
│   │   │   └── Notification.vue
│   │   │
│   │   ├── layout/             # Layout Components
│   │   │   ├── Header.vue
│   │   │   ├── Sidebar.vue
│   │   │   ├── Footer.vue
│   │   │   └── MainLayout.vue
│   │   │
│   │   ├── dashboard/          # Dashboard Widgets
│   │   │   ├── QuickStats.vue
│   │   │   ├── UpcomingEvents.vue
│   │   │   ├── RecentFiles.vue
│   │   │   └── DeadlineWidget.vue
│   │   │
│   │   ├── files/              # File Components
│   │   │   ├── FileUploader.vue
│   │   │   ├── FileList.vue
│   │   │   ├── FileCard.vue
│   │   │   └── FileViewer.vue
│   │   │
│   │   ├── calendar/           # Calendar Components
│   │   │   ├── CalendarView.vue
│   │   │   ├── EventCard.vue
│   │   │   └── CreateEventModal.vue
│   │   │
│   │   ├── lecture/            # Live Lecture Components
│   │   │   ├── LectureRecorder.vue
│   │   │   ├── TranscriptView.vue
│   │   │   ├── ConceptHighlight.vue
│   │   │   └── LectureControls.vue
│   │   │
│   │   └── tutor/              # Tutor Mode Components
│   │       ├── StudySession.vue
│   │       ├── QuizCard.vue
│   │       ├── FlashcardStack.vue
│   │       └── ProgressTracker.vue
│   │
│   ├── views/                  # Page Components (Routes)
│   │   ├── Home.vue
│   │   ├── Dashboard.vue
│   │   ├── Login.vue
│   │   ├── Register.vue
│   │   ├── Files.vue
│   │   ├── Calendar.vue
│   │   ├── Mail.vue
│   │   ├── Lecture.vue
│   │   ├── Tutor.vue
│   │   ├── Settings.vue
│   │   └── Profile.vue
│   │
│   ├── router/                 # Vue Router
│   │   └── index.ts            # Route Definitions
│   │
│   ├── stores/                 # Pinia State Management
│   │   ├── auth.ts             # Authentication State
│   │   ├── user.ts             # User Data
│   │   ├── files.ts            # File Management
│   │   ├── calendar.ts         # Calendar Events
│   │   ├── lecture.ts          # Live Lecture State
│   │   └── tutor.ts            # Tutor Mode State
│   │
│   ├── services/               # API Services
│   │   ├── api.ts              # Axios Instance
│   │   ├── auth.service.ts
│   │   ├── file.service.ts
│   │   ├── calendar.service.ts
│   │   ├── mail.service.ts
│   │   ├── lecture.service.ts
│   │   └── tutor.service.ts
│   │
│   ├── composables/            # Reusable Composition Functions
│   │   ├── useAuth.ts
│   │   ├── useFileUpload.ts
│   │   ├── useWebSocket.ts
│   │   ├── useNotifications.ts
│   │   ├── useLecture.ts
│   │   └── useTutor.ts
│   │
│   ├── utils/                  # Utility Functions
│   │   ├── formatters.ts       # Date, Number Formatting
│   │   ├── validators.ts       # Form Validation
│   │   ├── helpers.ts          # Helper Functions
│   │   └── constants.ts        # Constants
│   │
│   ├── types/                  # TypeScript Definitions
│   │   ├── api.types.ts
│   │   ├── user.types.ts
│   │   ├── file.types.ts
│   │   └── lecture.types.ts
│   │
│   └── plugins/                # Vue Plugins
│       └── vuetify.ts          # UI Framework
│
├── index.html                  # HTML Entry
├── vite.config.ts              # Vite Configuration
├── tsconfig.json               # TypeScript Config
├── package.json                # Dependencies
└── .eslintrc.js                # ESLint Config
```

### Frontend Architektur-Prinzipien

#### 1. **Component-Based Architecture**

```
App.vue
└── MainLayout.vue
    ├── Header.vue
    ├── Sidebar.vue
    └── RouterView
        └── Dashboard.vue
            ├── QuickStats.vue
            ├── UpcomingEvents.vue
            └── RecentFiles.vue
```

#### 2. **State Management (Pinia)**

```typescript
// stores/auth.ts
export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null as User | null,
    token: localStorage.getItem('token'),
    isAuthenticated: false
  }),
  
  actions: {
    async login(email: string, password: string) {
      const response = await authService.login(email, password)
      this.user = response.user
      this.token = response.token
      this.isAuthenticated = true
      localStorage.setItem('token', response.token)
    }
  }
})
```

#### 3. **Composables Pattern**

```typescript
// composables/useFileUpload.ts
export function useFileUpload() {
  const files = ref<File[]>([])
  const isUploading = ref(false)
  const progress = ref(0)
  
  const upload = async (file: File) => {
    isUploading.value = true
    // Upload logic
  }
  
  return { files, isUploading, progress, upload }
}
```

---

## 🗄️ Database (`database/`)

```
database/
├── migrations/                 # EF Core Migrations
│   ├── 20240101000000_InitialCreate.cs
│   ├── 20240102000000_AddLectureTables.cs
│   └── ...
│
├── seeds/                      # Seed Data
│   ├── SeedUsers.sql
│   ├── SeedCourses.sql
│   └── SeedData.cs
│
└── scripts/                    # Utility Scripts
    ├── backup.sh
    ├── restore.sh
    └── cleanup.sql
```

### Datenbank-Schema

**Haupttabellen:**

```sql
-- Users
CREATE TABLE Users (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Name VARCHAR(255),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Documents
CREATE TABLE Documents (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    FileName VARCHAR(255) NOT NULL,
    FileType VARCHAR(50),
    FileSize BIGINT,
    StoragePath VARCHAR(500),
    Title VARCHAR(255),
    Summary TEXT,
    CourseId INT,
    UploadedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Courses
CREATE TABLE Courses (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(255) NOT NULL,
    Code VARCHAR(50),
    Semester VARCHAR(20),
    Professor VARCHAR(255)
);

-- Events
CREATE TABLE Events (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Description TEXT,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    Location VARCHAR(255),
    GoogleEventId VARCHAR(255),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- LectureSessions (Live-Lecture-Mode)
CREATE TABLE LectureSessions (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    CourseId INT,
    Title VARCHAR(255),
    StartTime DATETIME NOT NULL,
    EndTime DATETIME,
    TranscriptPath VARCHAR(500),
    AudioPath VARCHAR(500),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
```

---

## 🧪 Tests (`tests/`)

```
tests/
├── Backend.Tests/
│   ├── Unit/
│   │   ├── Services/
│   │   │   ├── FileServiceTests.cs
│   │   │   └── AIServiceTests.cs
│   │   │
│   │   └── Helpers/
│   │       └── PdfHelperTests.cs
│   │
│   ├── Integration/
│   │   ├── Controllers/
│   │   │   └── FilesControllerTests.cs
│   │   │
│   │   └── Repositories/
│   │       └── DocumentRepositoryTests.cs
│   │
│   └── E2E/
│       └── FileUploadFlowTests.cs
│
└── Frontend.Tests/
    ├── unit/
    │   ├── components/
    │   │   └── FileUploader.spec.ts
    │   │
    │   └── stores/
    │       └── auth.spec.ts
    │
    └── e2e/
        └── login.spec.ts
```

---

## 📚 Docs (`docs/`)

```
docs/
├── architecture.md             # System Architektur
├── api.md                      # API Dokumentation
├── database.md                 # DB Schema
├── deployment.md               # Deployment Guide
├── live-lecture-guide.md       # Live Lecture Setup
├── tutor-mode-guide.md         # Tutor Mode Setup
└── troubleshooting.md          # Problembehebung
```

---

## 🎯 Best Practices

### Wo gehört was hin?

#### Neue API Endpoint hinzufügen
1. Controller in `API/Controllers/`
2. Service in `Core/Services/`
3. Interface in `Core/Interfaces/`
4. DTO in `Core/DTOs/`
5. Tests in `tests/Backend.Tests/`

#### Neue Frontend-Page hinzufügen
1. View in `views/`
2. Route in `router/index.ts`
3. Store in `stores/` (falls nötig)
4. Components in `components/`

#### Neue External API integrieren
1. Client in `Infrastructure/ExternalAPIs/`
2. Service in `Core/Services/`
3. Configuration in `.env`

---

**Diese Struktur ist optimiert für:**
- ✅ Skalierbarkeit
- ✅ Wartbarkeit
- ✅ Testbarkeit
- ✅ Team-Collaboration
