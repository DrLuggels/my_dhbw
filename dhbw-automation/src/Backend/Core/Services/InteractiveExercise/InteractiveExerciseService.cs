using DHBWAutomation.Backend.Core.Interfaces;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using DHBWAutomation.Backend.Shared.Helpers;
using Ganss.Xss;

namespace DHBWAutomation.Backend.Core.Services.InteractiveExercise;

public partial class InteractiveExerciseService : IInteractiveExerciseService
{
    private readonly AppDbContext _context;
    private readonly ILogger<InteractiveExerciseService> _logger;
    private readonly IAIService _aiService;
    private readonly AiMetrics _aiMetrics;
    private readonly HtmlSanitizer _htmlSanitizer;
    private readonly EncryptionHelper _encryptionHelper;

    private const string GeminiModel = "gemini-3-flash";

    public InteractiveExerciseService(
        AppDbContext context,
        ILogger<InteractiveExerciseService> logger,
        IAIService aiService,
        AiMetrics aiMetrics,
        EncryptionHelper encryptionHelper)
    {
        _context = context;
        _logger = logger;
        _aiService = aiService;
        _aiMetrics = aiMetrics;
        _encryptionHelper = encryptionHelper;

        _htmlSanitizer = new HtmlSanitizer();
        ConfigureHtmlSanitizer();
    }

    public async Task<Models.InteractiveExercise?> GetInteractiveExerciseAsync(int exerciseId)
    {
        return await _context.InteractiveExercises.FindAsync(exerciseId);
    }

    public string DetermineExerciseType(string difficulty, bool isNewConcept, bool isExamPrep)
    {
        if (isExamPrep) return "classic";
        if (isNewConcept || difficulty == "easy") return "interactive";
        if (difficulty == "hard") return "classic";
        return "interactive";
    }
}
