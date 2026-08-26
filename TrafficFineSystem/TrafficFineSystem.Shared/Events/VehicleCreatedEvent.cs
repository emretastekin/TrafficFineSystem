namespace TrafficFineSystem.Shared.Events;

public class VehicleCreatedEvent
{
    public int VehicleId { get; set; }
    public string Plate { get; set; } = string.Empty;
    public DateTime ProcessDate { get; set; }
}