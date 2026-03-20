namespace CubeManager.Core.Models;

public class Reservation
{
    public int Id { get; set; }
    public string ReservationDate { get; set; } = string.Empty;
    public string? TimeSlot { get; set; }
    public string? ThemeName { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public int Headcount { get; set; }
    public string Status { get; set; } = "confirmed";
    public string? RawHtml { get; set; }
    public DateTime? SyncedAt { get; set; }
}
