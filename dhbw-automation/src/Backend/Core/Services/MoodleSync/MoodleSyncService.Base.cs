using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;
using DHBWAutomation.Backend.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services.MoodleSync;

/// <summary>
/// Basis-Klasse für alle partial Moodle Sync Service Teile
/// </summary>
public partial class MoodleSyncService : IMoodleSyncService
{
    protected readonly AppDbContext _context;
    protected readonly MoodleApiClient _moodleClient;
    protected readonly ILogger<MoodleSyncService> _logger;
    protected readonly EncryptionHelper _encryptionHelper;
    private new readonly IServiceProvider? _serviceProvider;

    public MoodleSyncService(
        AppDbContext context,
        MoodleApiClient moodleClient,
        ILogger<MoodleSyncService> logger,
        EncryptionHelper encryptionHelper,
        IServiceProvider? serviceProvider = null)
    {
        _context = context;
        _moodleClient = moodleClient;
        _logger = logger;
        _encryptionHelper = encryptionHelper;
        _serviceProvider = serviceProvider;
    }
}
