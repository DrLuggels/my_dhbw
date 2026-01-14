namespace DHBWAutomation.Backend.Core.Services.Embedding;

public class SemanticSearchResult
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public float Score { get; set; }
    public int? UserId { get; set; }
}
