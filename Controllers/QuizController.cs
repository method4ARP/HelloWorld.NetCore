using Microsoft.AspNetCore.Mvc;
using HelloWorld.NetCore.Models;
using System.Text.Json;

namespace HelloWorld.NetCore.Controllers;

public class QuizController : Controller
{
    private static QuizSession? _currentSession;
    private readonly IWebHostEnvironment _environment;

    public QuizController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    private List<QuizQuestion> GetAZ204Questions()
    {
        try
        {
            var jsonPath = Path.Combine(_environment.ContentRootPath, "Data", "az204-questions.json");
            var jsonContent = System.IO.File.ReadAllText(jsonPath);
            var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return questions ?? new List<QuizQuestion>();
        }
        catch (Exception ex)
        {
            // Log error and return empty list
            Console.WriteLine($"Error loading questions: {ex.Message}");
            return new List<QuizQuestion>();
        }
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Start()
    {
        _currentSession = new QuizSession
        {
            Questions = GetAZ204Questions(),
            StartTime = DateTime.Now,
            CurrentQuestionIndex = 0,
            IsCompleted = false
        };

        return RedirectToAction(nameof(Question));
    }

    public IActionResult Question()
    {
        if (_currentSession == null || _currentSession.IsCompleted)
        {
            return RedirectToAction(nameof(Index));
        }

        if (_currentSession.CurrentQuestionIndex >= _currentSession.Questions.Count)
        {
            return RedirectToAction(nameof(Results));
        }

        var currentQuestion = _currentSession.Questions[_currentSession.CurrentQuestionIndex];
        ViewBag.QuestionNumber = _currentSession.CurrentQuestionIndex + 1;
        ViewBag.TotalQuestions = _currentSession.Questions.Count;
        
        return View(currentQuestion);
    }

    [HttpPost]
    public IActionResult SubmitAnswer(int questionId, int answerIndex)
    {
        if (_currentSession == null)
        {
            return RedirectToAction(nameof(Index));
        }

        _currentSession.UserAnswers[questionId] = answerIndex;
        _currentSession.CurrentQuestionIndex++;

        return RedirectToAction(nameof(Question));
    }

    public IActionResult Results()
    {
        if (_currentSession == null)
        {
            return RedirectToAction(nameof(Index));
        }

        _currentSession.IsCompleted = true;
        var timeTaken = DateTime.Now - _currentSession.StartTime;

        var questionResults = new List<QuestionResult>();
        int correctCount = 0;

        foreach (var question in _currentSession.Questions)
        {
            var userAnswerIndex = _currentSession.UserAnswers.ContainsKey(question.Id) 
                ? _currentSession.UserAnswers[question.Id] 
                : -1;
            
            var isCorrect = userAnswerIndex == question.CorrectAnswerIndex;
            if (isCorrect) correctCount++;

            questionResults.Add(new QuestionResult
            {
                Question = question,
                UserAnswerIndex = userAnswerIndex,
                IsCorrect = isCorrect
            });
        }

        var result = new QuizResultViewModel
        {
            TotalQuestions = _currentSession.Questions.Count,
            CorrectAnswers = correctCount,
            IncorrectAnswers = _currentSession.Questions.Count - correctCount,
            ScorePercentage = Math.Round((double)correctCount / _currentSession.Questions.Count * 100, 2),
            TimeTaken = timeTaken,
            QuestionResults = questionResults
        };

        return View(result);
    }

    [HttpPost]
    public IActionResult Restart()
    {
        _currentSession = null;
        return RedirectToAction(nameof(Index));
    }
}
