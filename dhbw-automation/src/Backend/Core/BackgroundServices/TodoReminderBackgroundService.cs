using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.BackgroundServices;

/// <summary>
/// Background service for sending reminders about overdue todos
/// </summary>
public class TodoReminderBackgroundService : BackgroundService
{
    private readonly ILogger<TodoReminderBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Configuration
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(1); // Check every hour
    private const int DaysBeforeFirstReminder = 7; // Remind after 7 days
    private const int DaysBetweenReminders = 3; // Send follow-up reminders every 3 days
    private const int MaxReminders = 3; // Maximum number of reminders per todo

    public TodoReminderBackgroundService(
        ILogger<TodoReminderBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Todo Reminder Background Service started");

        // Initial delay to let the system start up
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendRemindersAsync(stoppingToken);
                await Task.Delay(RunInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Todo Reminder Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Todo Reminder Background Service");
                // Wait before retry
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task CheckAndSendRemindersAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reminderCount = 0;
        var now = DateTime.UtcNow;

        try
        {
            // Find overdue todos that need reminders
            var cutoffDate = now.AddDays(-DaysBeforeFirstReminder);

            var overdueTodos = await context.Todos
                .Where(t => t.ArchivedAt == null
                    && t.Status == "pending"
                    && t.CreatedAt < cutoffDate
                    && t.ReminderCount < MaxReminders)
                .Include(t => t.User)
                .ToListAsync(stoppingToken);

            foreach (var todo in overdueTodos)
            {
                // Check if it's time for a reminder
                var shouldSendReminder = false;

                if (todo.LastReminderSent == null)
                {
                    // First reminder
                    shouldSendReminder = true;
                }
                else
                {
                    // Follow-up reminder
                    var nextReminderDate = todo.LastReminderSent.Value.AddDays(DaysBetweenReminders);
                    shouldSendReminder = now >= nextReminderDate;
                }

                if (shouldSendReminder)
                {
                    await CreateReminderInteractionAsync(context, todo, stoppingToken);

                    todo.LastReminderSent = now;
                    todo.ReminderCount++;
                    reminderCount++;
                }
            }

            if (reminderCount > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Created {reminderCount} todo reminders");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for overdue todos");
            throw;
        }
    }

    private async Task CreateReminderInteractionAsync(AppDbContext context, Todo todo, CancellationToken stoppingToken)
    {
        var daysOld = (DateTime.UtcNow - todo.CreatedAt).Days;

        // Calculate a suggested date (next available weekday)
        var suggestedDate = DateTime.UtcNow.AddDays(1);
        while (suggestedDate.DayOfWeek == DayOfWeek.Saturday || suggestedDate.DayOfWeek == DayOfWeek.Sunday)
        {
            suggestedDate = suggestedDate.AddDays(1);
        }

        var interaction = new UserInteraction
        {
            UserId = todo.UserId,
            Type = "todo_reminder",
            Priority = todo.Priority == "urgent" ? "high" : (todo.Priority == "high" ? "medium" : "low"),
            Title = $"Erinnerung: {todo.Title}",
            Message = $"Diese Aufgabe ist seit {daysOld} Tagen offen. Möchtest du einen Termin dafür einplanen?",
            ActionOptions = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                new { action = "schedule", label = $"Termin am {suggestedDate:dd.MM.yyyy} eintragen", suggestedDate = suggestedDate.ToString("o") },
                new { action = "snooze", label = "In 3 Tagen erinnern" },
                new { action = "complete", label = "Als erledigt markieren" },
                new { action = "dismiss", label = "Ignorieren" }
            }),
            Status = "pending",
            RelatedTodoId = todo.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.UserInteractions.Add(interaction);
        _logger.LogInformation($"Created reminder for todo {todo.Id}: {todo.Title} (user {todo.UserId})");
    }
}
