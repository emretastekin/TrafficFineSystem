using System.ComponentModel.DataAnnotations;

namespace TrafficFineSystem.WebApp.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Lütfen e-posta adresinizi giriniz.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lütfen şifrenizi giriniz.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}