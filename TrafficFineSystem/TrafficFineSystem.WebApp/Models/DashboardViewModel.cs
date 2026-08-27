namespace TrafficFineSystem.WebApp.Models;

public class DashboardViewModel
{
    public decimal TotalPaidAmount { get; set; }
    public int PaidFinesCount { get; set; } // YENİ EKLENEN
    public int PendingApprovals { get; set; }
    public int NewFines { get; set; }
    public int CanceledFines { get; set; }
    public int TotalVehicles { get; set; } // YENİ EKLENEN
    
    public Dictionary<string, int> VehicleTypeStats { get; set; } = new();
}