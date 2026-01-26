using Microsoft.AspNetCore.Mvc;
using HelloWorld.NetCore.Models;
using HelloWorld.NetCore.Services;

namespace HelloWorld.NetCore.Controllers
{
    public class BibleController : Controller
    {
        private readonly BibleReadingAIService _aiService;

        public BibleController(BibleReadingAIService aiService)
        {
            _aiService = aiService;
        }

        public async Task<IActionResult> Index(string ageGroup = "Adult", string gender = "All")
        {
            var readings = await _aiService.GenerateReadingsAsync(ageGroup, gender);
            
            var readingPlan = new BibleReadingPlan
            {
                Week = GetCurrentWeekNumber(),
                Readings = readings
            };

            return View(readingPlan);
        }

        private int GetCurrentWeekNumber()
        {
            var today = DateTime.Today;
            var jan1 = new DateTime(today.Year, 1, 1);
            var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
            var firstMonday = jan1.AddDays(daysOffset);
            var weekNumber = (today - firstMonday).Days / 7 + 1;
            return weekNumber;
        }
    }
}
