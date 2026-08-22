using TrafficFineSystem.Shared.Enums;

namespace TrafficFineSystem.Shared.Entities;

public class FineHistory
{
    public int Id { get; set; }
    public int TrafficFineId { get; set; }
    public string UserId { get; set; } = string.Empty; // İşlemi gerçekleştiren kullanıcı (Firebase'den gelecek)
    public DateTime ProcessDate { get; set; } // İşlem Tarihi
    public string ProcessType { get; set; } = string.Empty; // Onaylandı / Reddedildi vs.
    public string? Reason { get; set; } // Açıklama / Ret nedeni (Nullable)
    public FineStatus PreviousStatus { get; set; } // Önceki Durum
    public FineStatus NewStatus { get; set; } // Yeni Durum
}