using TrafficFineSystem.Shared.Enums;

namespace TrafficFineSystem.Shared.Entities;

public class Vehicle
{
    public int Id { get; set; }
    public string Plate { get; set; } = string.Empty; // Plaka
    public VehicleType Type { get; set; }             // Araç Tipi
    public string BrandModel { get; set; } = string.Empty; // Marka / Model

    // Bir aracın birden fazla cezası olabilir (One-to-Many)
    public ICollection<TrafficFine> Fines { get; set; } = new List<TrafficFine>();
}