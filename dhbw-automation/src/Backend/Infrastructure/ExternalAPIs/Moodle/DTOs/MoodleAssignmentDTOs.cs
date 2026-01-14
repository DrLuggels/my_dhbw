namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle assignments
/// </summary>

public class MoodleAssignmentsResponse
{
    public List<MoodleAssignmentCourse>? Courses { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleAssignmentCourse
{
    public int Id { get; set; }
    public string Fullname { get; set; } = string.Empty;
    public string Shortname { get; set; } = string.Empty;
    public List<MoodleAssignmentData>? Assignments { get; set; }
}

public class MoodleAssignmentData
{
    public int Id { get; set; }
    public int Cmid { get; set; }
    public int Course { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Intro { get; set; }
    public long Duedate { get; set; }
    public long Cutoffdate { get; set; }
    public long Allowsubmissionsfromdate { get; set; }
    public int Grade { get; set; }
    public string? Submissiondrafts { get; set; }
    public bool Teamsubmission { get; set; }
}

public class MoodleSubmissionStatus
{
    public MoodleLastAttempt? Lastattempt { get; set; }
    public MoodleFeedback? Feedback { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleLastAttempt
{
    public MoodleSubmission? Submission { get; set; }
    public bool Submissionsenabled { get; set; }
    public bool Locked { get; set; }
    public bool Graded { get; set; }
    public bool Canedit { get; set; }
    public bool Cansubmit { get; set; }
}

public class MoodleSubmission
{
    public int Id { get; set; }
    public int Userid { get; set; }
    public string Status { get; set; } = string.Empty;
    public long Timecreated { get; set; }
    public long Timemodified { get; set; }
}

public class MoodleFeedback
{
    public MoodleGrade? Grade { get; set; }
    public string? Gradefordisplay { get; set; }
}

public class MoodleGrade
{
    public int Id { get; set; }
    public string? Grade { get; set; }
    public long Timecreated { get; set; }
    public long Timemodified { get; set; }
}
