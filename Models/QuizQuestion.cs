namespace HelloWorld.NetCore.Models;

public class QuizQuestion
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswerIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
}

public class QuizSession
{
    public List<QuizQuestion> Questions { get; set; } = new();
    public Dictionary<int, int> UserAnswers { get; set; } = new();
    public int CurrentQuestionIndex { get; set; } = 0;
    public DateTime StartTime { get; set; }
    public bool IsCompleted { get; set; }
}

public class QuizResultViewModel
{
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int IncorrectAnswers { get; set; }
    public double ScorePercentage { get; set; }
    public TimeSpan TimeTaken { get; set; }
    public List<QuestionResult> QuestionResults { get; set; } = new();
}

public class QuestionResult
{
    public QuizQuestion Question { get; set; } = new();
    public int UserAnswerIndex { get; set; }
    public bool IsCorrect { get; set; }
}
