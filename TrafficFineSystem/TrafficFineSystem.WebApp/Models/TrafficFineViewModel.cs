using System.Text.Json.Serialization;

namespace TrafficFineSystem.WebApp.Models;

public class TrafficFineViewModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("issueDate")]
    public DateTime IssueDate { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; } // 1: Yeni, 2: Ödendi, 3: İptal (Enuma karşılık gelir)

    [JsonPropertyName("vehicleId")]
    public int VehicleId { get; set; }
}