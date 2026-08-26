using System.ComponentModel.DataAnnotations;

namespace TrafficFineSystem.WebApp.Models;

public class VehicleViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Plaka alanı zorunludur.")]
    public string Plate { get; set; } = string.Empty;

    [Required(ErrorMessage = "Araç tipi zorunludur.")]
    public int Type { get; set; } // Core.API'deki VehicleType enum'ı JSON'da int olarak gelir

    [Required(ErrorMessage = "Marka / Model alanı zorunludur.")]
    public string BrandModel { get; set; } = string.Empty;
}