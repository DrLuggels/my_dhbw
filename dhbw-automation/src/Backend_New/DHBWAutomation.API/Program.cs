using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using DHBWAutomation.Infrastructure.Database;
using DHBWAutomation.Core.Interfaces;
using DHBWAutomation.Infrastructure.Services;
using DHBWAutomation.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = Environments.Development
});

// Disable service validation during development
if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = false;
        options.ValidateOnBuild = false;
    });
}

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
        policy.WithOrigins(
                "http://localhost:5173",
                "http://192.168.178.198:8091",
                Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5173"
            )
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

    // Support for IFormFile in Swagger
    c.OperationFilter<SwaggerFileOperationFilter>();
});

// Database Context
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var dbDatabase = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "dhbw_automation";
var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "dhbw_user";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "dhbw_password";

var connectionString = $"Server={dbHost};Port={dbPort};Database={dbDatabase};User={dbUsername};Password={dbPassword};SslMode=None;";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 21)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    );
}, ServiceLifetime.Scoped);

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{Environment.GetEnvironmentVariable("REDIS_HOST")}:{Environment.GetEnvironmentVariable("REDIS_PORT")}";
    options.InstanceName = "DHBWAutomation_";
});

// Authentication & Authorization
// TODO: JWT Configuration vollständig implementieren
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options => { ... });

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IStorageService, MinIOStorageService>();
builder.Services.AddScoped<IRaplaService, RaplaService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();

// Background Workers
// TEMPORARILY DISABLED due to build issues
// builder.Services.AddHostedService<EmailSyncBackgroundService>();
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

// HTTP Client für allgemeine Zwecke (Rapla, etc.)
builder.Services.AddHttpClient();

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
// Database Migration (Development only)
// =============================================================================

// TEMPORARILY DISABLED: Start without database dependency
/*
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    try
    {
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("✅ Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database migration failed: {ex.Message}");
    }
}
*/
Console.WriteLine("⚠️ Database migration disabled - API starting without database");

// =============================================================================
// Run Application
// =============================================================================

Console.WriteLine("🚀 Starting DHBW Study Automation API...");
Console.WriteLine($"📍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🔗 API URL: {Environment.GetEnvironmentVariable("API_URL")}");
Console.WriteLine($"📚 Swagger UI: {Environment.GetEnvironmentVariable("API_URL")}/swagger");

app.Run();

