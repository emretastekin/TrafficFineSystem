using System.ComponentModel.DataAnnotations;
using TrafficFineSystem.Shared.Enums;

namespace TrafficFineSystem.Shared.Entities;

public class Vehicle
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Plaka alanı zorunludur.")]
    [RegularExpression(@"^(0[1-9]|[1-7][0-9]|8[0-1])\s?[A-Za-z]{1,3}\s?\d{2,4}$", 
        ErrorMessage = "Lütfen geçerli bir Türkiye plakası giriniz (Örn: 34 ABC 123).")]
    public string Plate { get; set; } = string.Empty;
    
    public VehicleType Type { get; set; }             // Araç Tipi
    public string BrandModel { get; set; } = string.Empty; // Marka / Model

    // Bir aracın birden fazla cezası olabilir (One-to-Many)
    public ICollection<TrafficFine> Fines { get; set; } = new List<TrafficFine>();
}