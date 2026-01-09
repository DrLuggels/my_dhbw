using System.Collections.Concurrent;

namespace DHBWAutomation.Backend.Shared.Helpers;

/// <summary>
/// Rate Limiter zur Begrenzung der Anzahl von API-Anfragen pro Zeitraum
/// </summary>
public class RateLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private readonly TimeSpan _period;
    private readonly ConcurrentQueue<DateTime> _requestTimes;
    private readonly int _maxRequests;
    private readonly object _lockObject = new();

    /// <summary>
    /// Erstellt einen neuen Rate Limiter
    /// </summary>
    /// <param name="maxRequests">Maximale Anzahl von Anfragen pro Zeitraum</param>
    /// <param name="period">Zeitraum für das Rate Limiting</param>
    public RateLimiter(int maxRequests, TimeSpan period)
    {
        _maxRequests = maxRequests;
        _period = period;
        _semaphore = new SemaphoreSlim(maxRequests, maxRequests);
        _requestTimes = new ConcurrentQueue<DateTime>();
    }

    /// <summary>
    /// Führt eine Aktion mit Rate Limiting aus
    /// </summary>
    /// <typeparam name="T">Rückgabetyp der Aktion</typeparam>
    /// <param name="action">Auszuführende Aktion</param>
    /// <returns>Ergebnis der Aktion</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        await WaitForAvailableSlotAsync();
        
        try
        {
            var result = await action();
            return result;
        }
        finally
        {
            ReleaseSlotAfterPeriod();
        }
    }

    /// <summary>
    /// Wartet, bis ein Slot verfügbar ist
    /// </summary>
    private async Task WaitForAvailableSlotAsync()
    {
        await _semaphore.WaitAsync();
        
        lock (_lockObject)
        {
            // Entferne alte Einträge außerhalb des Zeitfensters
            while (_requestTimes.TryPeek(out var oldestTime))
            {
                if (DateTime.UtcNow - oldestTime > _period)
                {
                    _requestTimes.TryDequeue(out _);
                }
                else
                {
                    break;
                }
            }
            
            _requestTimes.Enqueue(DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Gibt den Slot nach dem definierten Zeitraum wieder frei
    /// </summary>
    private void ReleaseSlotAfterPeriod()
    {
        _ = Task.Delay(_period).ContinueWith(t => _semaphore.Release());
    }

    /// <summary>
    /// Gibt die aktuelle Anzahl verfügbarer Slots zurück
    /// </summary>
    public int AvailableSlots => _semaphore.CurrentCount;

    /// <summary>
    /// Gibt die Anzahl der Anfragen im aktuellen Zeitfenster zurück
    /// </summary>
    public int CurrentRequestCount
    {
        get
        {
            lock (_lockObject)
            {
                // Entferne alte Einträge
                while (_requestTimes.TryPeek(out var oldestTime))
                {
                    if (DateTime.UtcNow - oldestTime > _period)
                    {
                        _requestTimes.TryDequeue(out _);
                    }
                    else
                    {
                        break;
                    }
                }
                
                return _requestTimes.Count;
            }
        }
    }
}
