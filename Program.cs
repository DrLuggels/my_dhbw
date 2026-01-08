using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// Configuration
// =============================================================================

// Load environment variables from .env file
DotNetEnv.Env.Load();

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
});

// Database Context
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = $"{Environment.GetEnvironmentVariable("REDIS_HOST")}:{Environment.GetEnvironmentVariable("REDIS_PORT")}";
    options.InstanceName = "DHBWAutomation_";
});

// Authentication & Authorization
// TODO: JWT Configuration

// Application Services
// builder.Services.AddScoped<IFileService, FileService>();
// builder.Services.AddScoped<IAIService, AIService>();
// builder.Services.AddScoped<ICalendarService, CalendarService>();
// builder.Services.AddScoped<IMailService, MailService>();

// Background Workers
// builder.Services.AddHostedService<MailPollerWorker>();
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
// Database Migration (Development only)
// =============================================================================

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

// =============================================================================
// Run Application
// =============================================================================

Console.WriteLine("🚀 Starting DHBW Study Automation API...");
Console.WriteLine($"📍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🔗 API URL: {Environment.GetEnvironmentVariable("API_URL")}");
Console.WriteLine($"📚 Swagger UI: {Environment.GetEnvironmentVariable("API_URL")}/swagger");

app.Run();

// =============================================================================
// AppDbContext Placeholder
// =============================================================================

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets werden hier hinzugefügt
    // public DbSet<User> Users { get; set; }
    // public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity Configurations
        // modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}
