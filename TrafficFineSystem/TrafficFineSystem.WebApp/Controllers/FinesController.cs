using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TrafficFineSystem.WebApp.Models;

namespace TrafficFineSystem.WebApp.Controllers;

public class FinesController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FinesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        // "CoreApi" isimli client'ı çağırıyoruz (Base URL'i Program.cs'te tanımlamıştık)
        var client = _httpClientFactory.CreateClient("CoreApi");
        
        // Core.API'deki GET /api/Fines endpoint'ine istek atıyoruz
        var response = await client.GetAsync("/api/Fines");

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            var fines = JsonSerializer.Deserialize<List<TrafficFineViewModel>>(jsonString);
            
            // Veriyi ekrana (View'a) gönderiyoruz
            return View(fines);
        }

        // Eğer API'ye ulaşılamazsa veya hata dönerse boş liste gönder
        return View(new List<TrafficFineViewModel>());
    }
}