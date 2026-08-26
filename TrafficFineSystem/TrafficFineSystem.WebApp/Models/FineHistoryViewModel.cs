namespace TrafficFineSystem.WebApp.Models;

public class FineHistoryViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime ProcessDate { get; set; }
    public string ProcessType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int PreviousStatus { get; set; }
    public int NewStatus { get; set; }
}