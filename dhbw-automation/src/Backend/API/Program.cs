using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Services;
using DHBWAutomation.Backend.Core.BackgroundServices;
using DHBWAutomation.Backend.Infrastructure.Storage;
using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Rapla;
using DHBWAutomation.Backend.Shared.Helpers;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// Configuration
// =============================================================================

// Load environment variables from .env file
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

var configuration = builder.Configuration;

// =============================================================================
// Services Configuration
// =============================================================================

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = new List<string>
        {
            "http://localhost:5173",
            "http://localhost:8091",
            "http://192.168.178.198:8091",
            "http://192.168.178.198:5173"
        };
        
        var envAppUrl = Environment.GetEnvironmentVariable("APP_URL");
        if (!string.IsNullOrEmpty(envAppUrl) && !allowedOrigins.Contains(envAppUrl))
        {
            allowedOrigins.Add(envAppUrl);
        }
        
        policy.WithOrigins(allowedOrigins.ToArray())
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger / OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DHBW Study Automation API",
        Version = "v1",
        Description = "API für das DHBW Study Automation System",
        Contact = new OpenApiContact
        {
            Name = "Dr. Luggels",
            Email = "your.email@example.com"
        }
    });

    // JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database Context
var dbProvider = Environment.GetEnvironmentVariable("DB_PROVIDER") ?? "sqlite";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider.ToLower() == "sqlite")
    {
        // SQLite für lokale Entwicklung
        var dbPath = Environment.GetEnvironmentVariable("SQLITE_DB_PATH") ?? "dhbw_automation.db";
        options.UseSqlite($"Data Source={dbPath}");
        Console.WriteLine($"📦 Using SQLite Database: {dbPath}");
    }
    else
    {
        // MariaDB/MySQL für Docker/Production
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
        var dbDatabase = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "dhbw_automation";
        var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "dhbw_user";
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "dhbw_password";

        var connectionString = $"Server={dbHost};Port={dbPort};Database={dbDatabase};User={dbUsername};Password={dbPassword};";

        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString),
            mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            )
        );
        Console.WriteLine($"📦 Using MariaDB Database: {dbHost}:{dbPort}/{dbDatabase}");
    }
});

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{Environment.GetEnvironmentVariable("REDIS_HOST")}:{Environment.GetEnvironmentVariable("REDIS_PORT")}";
    options.InstanceName = "DHBWAutomation_";
});

// Authentication & Authorization
// JWT Secret einmalig beim Start laden
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") 
    ?? builder.Configuration["JWT_SECRET"]
    ?? "your-super-secret-jwt-key-change-this-in-production-min-32-chars";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "DHBWAutomation";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "DHBWAutomationUsers";
var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

Console.WriteLine($"[JWT CONFIG] Using JWT_SECRET (length: {jwtSecret.Length}), Issuer: {jwtIssuer}, Audience: {jwtAudience}");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = securityKey,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
        
        // Detailliertes Logging für Debugging
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT Auth failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("JWT Token validated successfully");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IStorageService, MinIOStorageService>();
builder.Services.AddScoped<IRaplaService, RaplaService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<ITravelService, HafasService>();

// AI-Related Services
builder.Services.AddScoped<IIntentAnalysisService, IntentAnalysisService>();
builder.Services.AddScoped<ILearningAnalyticsService, LearningAnalyticsService>();
builder.Services.AddScoped<IDocumentParsingService, DocumentParsingService>();
builder.Services.AddScoped<IValidationService, ValidationService>(); // NEW: AI Staging System

// Calendar Services
builder.Services.AddScoped<ISchedulingService, SchedulingService>();
builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();

// Helper Services
builder.Services.AddSingleton<AnthropicClient>();
builder.Services.AddSingleton<AiMetrics>();
builder.Services.AddSingleton<EncryptionHelper>();

// Rapla Client
builder.Services.AddHttpClient<RaplaClient>();

// HAFAS Travel Service
builder.Services.AddHttpClient<HafasService>();

// Background Workers
builder.Services.AddHostedService<EmailSyncBackgroundService>();
builder.Services.AddSingleton<DocumentProcessingBackgroundService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<DocumentProcessingBackgroundService>());
builder.Services.AddHostedService<PeriodicReviewBackgroundService>(); // Daily fundamental knowledge review
// TODO: Weitere Background Workers implementieren
// builder.Services.AddHostedService<MoodleSyncWorker>();

// HTTP Clients
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.DefaultRequestHeaders.Add("Authorization", 
        $"Bearer {Environment.GetEnvironmentVariable("OPENAI_API_KEY")}");
});

builder.Services.AddHttpClient("Anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/v1/");
    client.DefaultRequestHeaders.Add("x-api-key", 
        Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// =============================================================================
// App Configuration
// =============================================================================

var app = builder.Build();

// Development Environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DHBW API v1");
        c.RoutePrefix = "swagger";
    });
}

// Exception Handler
app.UseExceptionHandler("/error");

// HTTPS Redirection
// app.UseHttpsRedirection();

// CORS
app.UseCors("AllowFrontend");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// Health Check
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}));

// Welcome Endpoint
app.MapGet("/", () => Results.Ok(new
{
    message = "🎓 DHBW Study Automation API",
    version = "1.0.0",
    documentation = "/swagger",
    health = "/health"
}));

// =============================================================================
// Database Initialization
// =============================================================================

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        // Für SQLite: Erstelle Datenbank automatisch
        if (dbProvider.ToLower() == "sqlite")
        {
            dbContext.Database.EnsureCreated();
            Console.WriteLine("✅ SQLite Database created/verified successfully");
        }
        else
        {
            // Für MariaDB: Verwende Migrationen
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("✅ Database migration completed successfully");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
    }
}

// =============================================================================
// API Key Validation
// =============================================================================

Console.WriteLine("\n🔑 Validating API Keys...");
var requiredKeys = new[] { "OPENAI_API_KEY", "ANTHROPIC_API_KEY", "GEMINI_API_KEY" };
var missingKeys = requiredKeys.Where(k => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(k))).ToList();

if (missingKeys.Any())
{
    Console.WriteLine($"⚠️  Missing API Keys: {string.Join(", ", missingKeys)}");
    Console.WriteLine("⚠️  Some AI features will be unavailable!");
}
else
{
    Console.WriteLine("✅ All AI API Keys configured");
}

// Validate optional keys
var optionalKeys = new Dictionary<string, string>
{
    { "GOOGLE_CLIENT_ID", "Google Calendar" },
    { "GOOGLE_CLIENT_SECRET", "Google Calendar" },
    { "SMTP_HOST", "Email Integration" }
};

foreach (var (key, feature) in optionalKeys)
{
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
    {
        Console.WriteLine($"ℹ️  Optional: {key} not configured ({feature} unavailable)");
    }
}

Console.WriteLine();

// =============================================================================
// Run Application
// =============================================================================

Console.WriteLine("🚀 Starting DHBW Study Automation API...");
Console.WriteLine($"📍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🔗 API URL: {Environment.GetEnvironmentVariable("API_URL")}");
Console.WriteLine($"📚 Swagger UI: {Environment.GetEnvironmentVariable("API_URL")}/swagger");

app.Run();
