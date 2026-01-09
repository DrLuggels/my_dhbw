using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Background Service für automatische E-Mail-Synchronisation (jede Minute)
/// </summary>
public class EmailSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailSyncBackgroundService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(1);

    public EmailSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EmailSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Sync Background Service gestartet");

        // Warte 10 Sekunden bevor der erste Sync startet
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllUsersEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim E-Mail-Sync");
            }

            // Warte bis zum nächsten Sync-Intervall
            await Task.Delay(_syncInterval, stoppingToken);
        }

        _logger.LogInformation("Email Sync Background Service beendet");
    }

    private async Task SyncAllUsersEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();

        // Hole nur Benutzer mit aktiviertem E-Mail-Sync
        var activeUsers = await context.Users
            .Where(u => u.IsActive && u.EmailSyncEnabled && u.EmailSyncAddress != null && u.EmailSyncPassword != null)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Starte E-Mail-Sync für {Count} Benutzer", activeUsers.Count);

        foreach (var userId in activeUsers)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var newEmailsCount = await mailService.SyncEmailsAsync(userId, cancellationToken);

                if (newEmailsCount > 0)
                {
                    _logger.LogInformation("User {UserId}: {Count} neue E-Mails synchronisiert", userId, newEmailsCount);

                    // Starte asynchrone Verarbeitung der neuen E-Mails
                    _ = Task.Run(async () => await ProcessNewEmailsAsync(userId, cancellationToken), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Sync für User {UserId}", userId);
            }
        }
    }

    private async Task ProcessNewEmailsAsync(int userId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();

        // Hole alle unverarbeiteten E-Mails des Benutzers
        var unprocessedEmails = await context.Emails
            .Where(e => e.UserId == userId && !e.IsProcessed)
            .OrderByDescending(e => e.ReceivedAt)
            .Take(10) // Maximal 10 E-Mails pro Durchlauf verarbeiten
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Verarbeite {Count} E-Mails für User {UserId}", unprocessedEmails.Count, userId);

        foreach (var email in unprocessedEmails)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await mailService.ProcessEmailAsync(email.Id, cancellationToken);
                _logger.LogDebug("E-Mail {EmailId} verarbeitet", email.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Verarbeiten von E-Mail {EmailId}", email.Id);
            }

            // Kleine Verzögerung zwischen Verarbeitungen, um API-Limits zu respektieren
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
}
