namespace DHBWAutomation.Backend.Core.Services.MoodleSync;

#region Result DTOs

public class MoodleLoginSyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? MoodleUserId { get; set; }
    public string? MoodleUsername { get; set; }
    public string? MoodleFullname { get; set; }
}

public class MoodleConnectionTestResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SiteName { get; set; }
    public string? Username { get; set; }
    public string? Fullname { get; set; }
    public int UserId { get; set; }
}

public class MoodleSyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Deleted { get; set; }
}

public class MoodleFullSyncResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public MoodleSyncResult? CoursesResult { get; set; }
    public MoodleSyncResult? AssignmentsResult { get; set; }
    public MoodleSyncResult? ResourcesResult { get; set; }
    public MoodleSyncResult? CalendarEventsResult { get; set; }
    public MoodleBatchDownloadResult? DownloadResult { get; set; }

    public bool HasErrors =>
        (CoursesResult != null && !CoursesResult.Success) ||
        (AssignmentsResult != null && !AssignmentsResult.Success) ||
        (ResourcesResult != null && !ResourcesResult.Success) ||
        (CalendarEventsResult != null && !CalendarEventsResult.Success);

    public string GetErrorSummary()
    {
        var errors = new List<string>();
        if (CoursesResult?.ErrorMessage != null) errors.Add($"Kurse: {CoursesResult.ErrorMessage}");
        if (AssignmentsResult?.ErrorMessage != null) errors.Add($"Aufgaben: {AssignmentsResult.ErrorMessage}");
        if (ResourcesResult?.ErrorMessage != null) errors.Add($"Ressourcen: {ResourcesResult.ErrorMessage}");
        if (CalendarEventsResult?.ErrorMessage != null) errors.Add($"Kalender: {CalendarEventsResult.ErrorMessage}");
        return string.Join("; ", errors);
    }
}

public class MoodleSyncStatus
{
    public bool IsConfigured { get; set; }
    public bool IsSyncEnabled { get; set; }
    public int? MoodleUserId { get; set; }
    public string? MoodleUsername { get; set; }
    public DateTime? LastSync { get; set; }
    public string? LastSyncError { get; set; }
    public int CoursesCount { get; set; }
    public int AssignmentsCount { get; set; }
    public int ResourcesCount { get; set; }
    public int CalendarEventsCount { get; set; }
    public int PendingAssignmentsCount { get; set; }
}

public class MoodleDownloadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? DocumentId { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
}

public class MoodleBatchDownloadResult
{
    public bool Success { get; set; }
    public int TotalResources { get; set; }
    public int DownloadedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<int> CreatedDocumentIds { get; set; } = new();
}

#endregion
