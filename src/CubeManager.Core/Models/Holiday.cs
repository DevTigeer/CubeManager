namespace CubeManager.Core.Models;

public class Holiday
{
    public int Id { get; set; }
    public string HolidayDate { get; set; } = string.Empty;  // YYYY-MM-DD
    public string? HolidayName { get; set; }
    public bool IsWeekend { get; set; }
    public int Year { get; set; }
}
