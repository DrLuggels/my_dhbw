namespace DHBWAutomation.Backend.Infrastructure.ExternalAPIs.Moodle;

/// <summary>
/// DTOs for Moodle quizzes
/// </summary>

public class MoodleQuizzesResponse
{
    public List<MoodleQuizData>? Quizzes { get; set; }
    public List<MoodleWarning>? Warnings { get; set; }
}

public class MoodleQuizData
{
    public int Id { get; set; }
    public int Course { get; set; }
    public int Coursemodule { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Intro { get; set; }
    public int Introformat { get; set; }
    public long Timeopen { get; set; }
    public long Timeclose { get; set; }
    public int Timelimit { get; set; }
    public string Overduehandling { get; set; } = string.Empty;
    public int Graceperiod { get; set; }
    public string Preferredbehaviour { get; set; } = string.Empty;
    public int Canredoquestions { get; set; }
    public int Attempts { get; set; }
    public int Attemptonlast { get; set; }
    public int Grademethod { get; set; }
    public int Decimalpoints { get; set; }
    public int Questiondecimalpoints { get; set; }
    public int Reviewattempt { get; set; }
    public int Reviewcorrectness { get; set; }
    public int Reviewmarks { get; set; }
    public int Reviewspecificfeedback { get; set; }
    public int Reviewgeneralfeedback { get; set; }
    public int Reviewrightanswer { get; set; }
    public int Reviewoverallfeedback { get; set; }
    public int Questionsperpage { get; set; }
    public string Navmethod { get; set; } = string.Empty;
    public int Shuffleanswers { get; set; }
    public int Sumgrades { get; set; }
    public int Grade { get; set; }
    public long Timecreated { get; set; }
    public long Timemodified { get; set; }
    public string? Password { get; set; }
    public string? Subnet { get; set; }
    public int Browsersecurity { get; set; }
    public int Delay1 { get; set; }
    public int Delay2 { get; set; }
    public int Showuserpicture { get; set; }
    public int Showblocks { get; set; }
    public int Completionattemptsexhausted { get; set; }
    public int Completionpass { get; set; }
    public int Allowofflineattempts { get; set; }
    public int Autosaveperiod { get; set; }
    public int Hasfeedback { get; set; }
    public int Hasquestions { get; set; }
    public int Section { get; set; }
    public bool Visible { get; set; }
    public List<MoodleModuleContent>? Introfiles { get; set; }
}
