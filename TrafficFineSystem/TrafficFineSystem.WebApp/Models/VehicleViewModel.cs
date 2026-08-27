using System.ComponentModel.DataAnnotations;

namespace TrafficFineSystem.WebApp.Models;

public class VehicleViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Plaka alanı zorunludur.")]
    [RegularExpression(@"^(0[1-9]|[1-7][0-9]|8[0-1])\s?[A-Za-z]{1,3}\s?\d{2,4}$", 
        ErrorMessage = "Lütfen geçerli bir Türkiye plakası giriniz (Örn: 34 ABC 123).")]
    public string Plate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Araç tipi zorunludur.")]
    public int Type { get; set; } // Core.API'deki VehicleType enum'ı JSON'da int olarak gelir

    [Required(ErrorMessage = "Marka / Model alanı zorunludur.")]
    public string BrandModel { get; set; } = string.Empty;
}