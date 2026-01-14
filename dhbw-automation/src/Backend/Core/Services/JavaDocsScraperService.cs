using System.Text.RegularExpressions;
using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.Database;
using LibGit2Sharp;
using Markdig;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using DHBWAutomation.Backend.Core.Services.Embedding;

namespace DHBWAutomation.Backend.Core.Services;

/// <summary>
/// Service for scraping exercises from jappuccini/java-docs GitHub repository
/// </summary>
public class JavaDocsScraperService : IJavaDocsScraperService
{
    private readonly AppDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<JavaDocsScraperService> _logger;
    private readonly IConfiguration _configuration;

    private const string RepoUrl = "https://github.com/jappuccini/java-docs.git";
    private const string DefaultLocalPath = "./data/java-docs-repo";

    public JavaDocsScraperService(
        AppDbContext context,
        IEmbeddingService embeddingService,
        ILogger<JavaDocsScraperService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _embeddingService = embeddingService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Clone or update the java-docs repository
    /// </summary>
    public async Task<string> CloneOrUpdateRepoAsync()
    {
        var localPath = _configuration["JavaDocs:LocalPath"] ?? DefaultLocalPath;

        try
        {
            if (Repository.IsValid(localPath))
            {
                // Pull latest changes
                _logger.LogInformation("Updating existing repository at {Path}", localPath);

                using var repo = new Repository(localPath);
                var signature = new Signature("DHBW Automation", "automation@dhbw.de", DateTimeOffset.Now);

                Commands.Pull(repo, signature, new PullOptions());

                var headCommit = repo.Head.Tip.Sha;
                _logger.LogInformation("Repository updated to commit {Commit}", headCommit);

                return headCommit;
            }
            else
            {
                // Clone repository
                _logger.LogInformation("Cloning repository to {Path}", localPath);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(localPath) ?? localPath);

                Repository.Clone(RepoUrl, localPath, new CloneOptions
                {
                    RecurseSubmodules = false
                });

                using var repo = new Repository(localPath);
                var headCommit = repo.Head.Tip.Sha;
                _logger.LogInformation("Repository cloned at commit {Commit}", headCommit);

                return headCommit;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning/updating repository");
            throw;
        }
    }

    /// <summary>
    /// Parse all MDX files and extract exercises
    /// </summary>
    public async Task<List<JavaDocsExercise>> ParseAllExercisesAsync()
    {
        var localPath = _configuration["JavaDocs:LocalPath"] ?? DefaultLocalPath;
        var exercises = new List<JavaDocsExercise>();

        try
        {
            // Find all MDX files in docs directory
            var docsPath = Path.Combine(localPath, "docs");
            if (!Directory.Exists(docsPath))
            {
                _logger.LogWarning("Docs directory not found at {Path}", docsPath);
                return exercises;
            }

            var mdxFiles = Directory.GetFiles(docsPath, "*.mdx", SearchOption.AllDirectories);
            _logger.LogInformation("Found {Count} MDX files", mdxFiles.Length);

            // Get current commit hash
            string? commitHash = null;
            if (Repository.IsValid(localPath))
            {
                using var repo = new Repository(localPath);
                commitHash = repo.Head.Tip.Sha;
            }

            foreach (var filePath in mdxFiles)
            {
                try
                {
                    var exercise = await ParseMdxFileAsync(filePath, localPath, commitHash);
                    if (exercise != null)
                    {
                        exercises.Add(exercise);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing file {Path}", filePath);
                }
            }

            _logger.LogInformation("Parsed {Count} exercises", exercises.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing exercises");
        }

        return exercises;
    }

    /// <summary>
    /// Sync exercises to database
    /// </summary>
    public async Task<SyncExercisesResult> SyncExercisesToDatabaseAsync()
    {
        var result = new SyncExercisesResult();

        try
        {
            // First, update repo
            var commitHash = await CloneOrUpdateRepoAsync();

            // Parse all exercises
            var exercises = await ParseAllExercisesAsync();

            // Get existing exercises
            var existingExercises = await _context.JavaDocsExercises.ToListAsync();
            var existingByPath = existingExercises.ToDictionary(e => e.FilePath);

            foreach (var exercise in exercises)
            {
                if (existingByPath.TryGetValue(exercise.FilePath, out var existing))
                {
                    // Update if content changed
                    if (existing.RawMdxContent != exercise.RawMdxContent)
                    {
                        existing.Title = exercise.Title;
                        existing.Topic = exercise.Topic;
                        existing.Subtopic = exercise.Subtopic;
                        existing.Difficulty = exercise.Difficulty;
                        existing.ExerciseType = exercise.ExerciseType;
                        existing.RawMdxContent = exercise.RawMdxContent;
                        existing.ParsedContent = exercise.ParsedContent;
                        existing.CodeSnippets = exercise.CodeSnippets;
                        existing.SolutionCode = exercise.SolutionCode;
                        existing.Tags = exercise.Tags;
                        existing.Frontmatter = exercise.Frontmatter;
                        existing.GitCommitHash = commitHash;
                        existing.LastUpdatedAt = DateTime.UtcNow;
                        existing.HasEmbedding = false; // Mark for re-embedding

                        result.Updated++;
                    }
                    else
                    {
                        result.Unchanged++;
                    }
                }
                else
                {
                    // New exercise
                    exercise.GitCommitHash = commitHash;
                    _context.JavaDocsExercises.Add(exercise);
                    result.Added++;
                }
            }

            await _context.SaveChangesAsync();

            // Generate embeddings for new/updated exercises
            var exercisesNeedingEmbedding = await _context.JavaDocsExercises
                .Where(e => !e.HasEmbedding)
                .Take(50) // Limit to avoid rate limiting
                .ToListAsync();

            foreach (var exercise in exercisesNeedingEmbedding)
            {
                try
                {
                    await _embeddingService.ProcessExerciseEmbeddingAsync(exercise.Id);
                    result.EmbeddingsGenerated++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate embedding for exercise {Id}", exercise.Id);
                }
            }

            result.Success = true;
            _logger.LogInformation("Sync completed: Added={Added}, Updated={Updated}, Embeddings={Embeddings}",
                result.Added, result.Updated, result.EmbeddingsGenerated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing exercises");
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Get all exercises with optional filtering
    /// </summary>
    public async Task<List<JavaDocsExercise>> GetExercisesAsync(
        string? topic = null,
        string? difficulty = null,
        string? searchQuery = null)
    {
        var query = _context.JavaDocsExercises.AsQueryable();

        if (!string.IsNullOrEmpty(topic))
        {
            query = query.Where(e => e.Topic == topic);
        }

        if (!string.IsNullOrEmpty(difficulty))
        {
            query = query.Where(e => e.Difficulty == difficulty);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(e =>
                e.Title.Contains(searchQuery) ||
                e.ParsedContent!.Contains(searchQuery) ||
                e.Topic.Contains(searchQuery));
        }

        return await query.OrderBy(e => e.Topic).ThenBy(e => e.Title).ToListAsync();
    }

    /// <summary>
    /// Get all available topics
    /// </summary>
    public async Task<List<string>> GetTopicsAsync()
    {
        return await _context.JavaDocsExercises
            .Select(e => e.Topic)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    private async Task<JavaDocsExercise?> ParseMdxFileAsync(string filePath, string basePath, string? commitHash)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var relativePath = Path.GetRelativePath(basePath, filePath).Replace("\\", "/");

        // Skip non-exercise files (like index files)
        if (relativePath.EndsWith("index.mdx") || relativePath.Contains("_category_"))
        {
            return null;
        }

        // Parse frontmatter
        var frontmatter = ParseFrontmatter(content);
        var title = frontmatter.GetValueOrDefault("title", Path.GetFileNameWithoutExtension(filePath));

        // Determine topic from path
        var pathParts = relativePath.Split('/');
        var topic = pathParts.Length > 1 ? pathParts[1] : "general";

        // Extract subtopic if available
        string? subtopic = null;
        if (pathParts.Length > 2)
        {
            subtopic = pathParts[2];
        }

        // Determine difficulty and type
        var difficulty = frontmatter.GetValueOrDefault("difficulty", "medium");
        var exerciseType = DetermineExerciseType(relativePath, frontmatter);

        // Parse content (remove frontmatter)
        var parsedContent = RemoveFrontmatter(content);

        // Extract code snippets
        var codeSnippets = ExtractCodeSnippets(parsedContent);

        // Extract solution if available
        var solutionCode = ExtractSolution(parsedContent);

        // Extract tags and convert to valid JSON array
        string? tags = null;
        if (frontmatter.TryGetValue("tags", out var tagValue) && !string.IsNullOrWhiteSpace(tagValue))
        {
            try
            {
                var trimmedValue = tagValue.Trim();

                // Handle YAML array format: [tag1, tag2] or [tag1]
                if (trimmedValue.StartsWith("[") && trimmedValue.EndsWith("]"))
                {
                    // Remove brackets and parse as comma-separated values
                    var innerContent = trimmedValue.Substring(1, trimmedValue.Length - 2);
                    var tagList = innerContent.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(t => t.Trim().Trim('"', '\''))
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();
                    tags = tagList.Count > 0 ? JsonSerializer.Serialize(tagList) : null;
                }
                else
                {
                    // Plain comma-separated string: tag1, tag2
                    var tagList = trimmedValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(t => t.Trim().Trim('"', '\''))
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();
                    tags = tagList.Count > 0 ? JsonSerializer.Serialize(tagList) : null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse tags: {TagValue}", tagValue);
                tags = null;
            }
        }

        return new JavaDocsExercise
        {
            FilePath = relativePath,
            Title = title,
            Topic = CapitalizeFirst(topic),
            Subtopic = subtopic != null ? CapitalizeFirst(subtopic) : null,
            Difficulty = difficulty,
            ExerciseType = exerciseType,
            RawMdxContent = content,
            ParsedContent = parsedContent,
            CodeSnippets = codeSnippets.Count > 0 ? JsonSerializer.Serialize(codeSnippets) : null,
            SolutionCode = solutionCode,
            Tags = tags,
            Frontmatter = frontmatter.Count > 0 ? JsonSerializer.Serialize(frontmatter) : null,
            GitCommitHash = commitHash
        };
    }

    private Dictionary<string, string> ParseFrontmatter(string content)
    {
        var frontmatter = new Dictionary<string, string>();

        var match = Regex.Match(content, @"^---\s*\n(.*?)\n---", RegexOptions.Singleline);
        if (match.Success)
        {
            var yaml = match.Groups[1].Value;
            var lines = yaml.Split('\n');

            foreach (var line in lines)
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = line.Substring(0, colonIndex).Trim();
                    var value = line.Substring(colonIndex + 1).Trim().Trim('"', '\'');
                    frontmatter[key] = value;
                }
            }
        }

        return frontmatter;
    }

    private string RemoveFrontmatter(string content)
    {
        return Regex.Replace(content, @"^---\s*\n.*?\n---\s*\n", "", RegexOptions.Singleline);
    }

    private List<CodeSnippet> ExtractCodeSnippets(string content)
    {
        var snippets = new List<CodeSnippet>();

        var matches = Regex.Matches(content, @"```(\w+)?\s*\n(.*?)\n```", RegexOptions.Singleline);
        foreach (Match match in matches)
        {
            snippets.Add(new CodeSnippet
            {
                Language = match.Groups[1].Value ?? "text",
                Code = match.Groups[2].Value.Trim()
            });
        }

        return snippets;
    }

    private string? ExtractSolution(string content)
    {
        // Look for solution blocks (various formats)
        var patterns = new[]
        {
            @"<Solution>\s*(.*?)\s*</Solution>",
            @"<details>\s*<summary>.*?[Ll]ösung.*?</summary>\s*(.*?)\s*</details>",
            @"## Lösung\s*(.*?)(?=##|$)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(content, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return null;
    }

    private string DetermineExerciseType(string path, Dictionary<string, string> frontmatter)
    {
        if (frontmatter.TryGetValue("type", out var type))
        {
            return type;
        }

        var lowerPath = path.ToLower();
        if (lowerPath.Contains("exam")) return "exam";
        if (lowerPath.Contains("exercise")) return "coding";
        if (lowerPath.Contains("theory")) return "theory";

        return "practice";
    }

    private string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1).Replace("-", " ");
    }
}

/// <summary>
/// Code snippet extracted from MDX
/// </summary>
public class CodeSnippet
{
    public string Language { get; set; } = "java";
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Result of sync operation
/// </summary>
public class SyncExercisesResult
{
    public bool Success { get; set; }
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public int EmbeddingsGenerated { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Interface for Java docs scraper
/// </summary>
public interface IJavaDocsScraperService
{
    Task<string> CloneOrUpdateRepoAsync();
    Task<List<JavaDocsExercise>> ParseAllExercisesAsync();
    Task<SyncExercisesResult> SyncExercisesToDatabaseAsync();
    Task<List<JavaDocsExercise>> GetExercisesAsync(string? topic = null, string? difficulty = null, string? searchQuery = null);
    Task<List<string>> GetTopicsAsync();
}
