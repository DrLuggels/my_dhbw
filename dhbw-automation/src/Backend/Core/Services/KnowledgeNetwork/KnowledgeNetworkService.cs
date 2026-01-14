using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Infrastructure.VectorDb;

namespace DHBWAutomation.Backend.Core.Services.KnowledgeNetwork;

/// <summary>
/// Service for managing the knowledge network ("Spinnennetz")
/// </summary>
public partial class KnowledgeNetworkService : IKnowledgeNetworkService
{
    private readonly AppDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantService _qdrantService;
    private readonly ILogger<KnowledgeNetworkService> _logger;

    private const double AutoLinkThreshold = 0.8;

    public KnowledgeNetworkService(
        AppDbContext context,
        IEmbeddingService embeddingService,
        IQdrantService qdrantService,
        ILogger<KnowledgeNetworkService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _logger = logger;
    }
}
