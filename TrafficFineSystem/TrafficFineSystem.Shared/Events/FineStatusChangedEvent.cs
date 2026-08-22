namespace TrafficFineSystem.Shared.Events;

public class FineStatusChangedEvent
{
    public int TrafficFineId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime ProcessDate { get; set; }
    public string ProcessType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int PreviousStatus { get; set; }
    public int NewStatus { get; set; }
}