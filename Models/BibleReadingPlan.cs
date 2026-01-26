namespace HelloWorld.NetCore.Models
{
    public class BibleReadingPlan
    {
        public int Week { get; set; }
        public List<DailyReading> Readings { get; set; } = new List<DailyReading>();
    }

    public class DailyReading
    {
        public string Day { get; set; } = string.Empty;
        public string Reading { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }
}
