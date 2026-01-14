using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DHBWAutomation.Backend.Core.Services.Mail;

public partial class MailService : IMailService
{
    private readonly AppDbContext _context;
    private readonly IAIService _aiService;
    private readonly IFileService _fileService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailService> _logger;

    public MailService(
        AppDbContext context,
        IAIService aiService,
        IFileService fileService,
        IConfiguration configuration,
        ILogger<MailService> logger)
    {
        _context = context;
        _aiService = aiService;
        _fileService = fileService;
        _configuration = configuration;
        _logger = logger;
    }
}
