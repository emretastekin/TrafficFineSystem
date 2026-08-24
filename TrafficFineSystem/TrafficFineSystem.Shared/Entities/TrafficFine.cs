using TrafficFineSystem.Shared.Enums;

namespace TrafficFineSystem.Shared.Entities;

public class TrafficFine
{
    public int Id { get; set; }
    public decimal Amount { get; set; } // Ceza Tutarı
    public DateTime IssueDate { get; set; } // Ceza Tarihi
    public FineStatus Status { get; set; } = FineStatus.Yeni; // Ceza Durumu
    
    // Yabancı Anahtar (Foreign Key)
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
}