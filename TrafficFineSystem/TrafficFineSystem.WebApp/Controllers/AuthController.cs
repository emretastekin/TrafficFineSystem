using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TrafficFineSystem.WebApp.Models;

namespace TrafficFineSystem.WebApp.Controllers;

public class AuthController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var apiKey = _configuration["Firebase:ApiKey"];
        
        // Firebase'in doğrudan giriş yapmak için sunduğu REST API uç noktası
        var requestUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

        var payload = new
        {
            email = model.Email,
            password = model.Password,
            returnSecureToken = true
        };

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(requestUrl, payload);

        if (response.IsSuccessStatusCode)
        {
            // Giriş başarılı, JSON içinden Token'ı koparıyoruz
            var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();
            var token = responseData.GetProperty("idToken").GetString();

            // Token'ı tarayıcının Cookie (Çerez) belleğine güvenlice kaydediyoruz
            var cookieOptions = new CookieOptions 
            { 
                Expires = DateTime.Now.AddHours(1), // 1 saat geçerli
                HttpOnly = true, // JavaScript ile çalınmasını engeller (Güvenlik)
                Secure = true // Sadece HTTPS üzerinden gider
            };
            
            Response.Cookies.Append("AuthToken", token!, cookieOptions);

            // Başarıyla giriş yaptıysa cezalar sayfasına yönlendir
            return RedirectToAction("Index", "Fines");
        }

        // Hata durumunda (Yanlış şifre vb.)
        ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı!");
        return View(model);
    }

    public IActionResult Logout()
    {
        // Çıkış yaparken Cookie'den token'ı siliyoruz
        Response.Cookies.Delete("AuthToken");
        return RedirectToAction("Login");
    }
}