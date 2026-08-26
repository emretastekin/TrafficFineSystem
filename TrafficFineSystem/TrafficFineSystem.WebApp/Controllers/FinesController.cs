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

        // YENİ KOD: Eğer başarısızsa hatayı ekrana fırlat ki ne olduğunu görelim!
        var errorMessage = await response.Content.ReadAsStringAsync();
        throw new Exception($"Core.API'den Hata Geldi! Durum Kodu: {response.StatusCode}. Detay: {errorMessage}");
        
    }
    
    
    // Yeni ceza ekleme sayfasını açar (GET)
    [HttpGet]
    public IActionResult Create()
    {
        return View(new TrafficFineViewModel());
    }

    // Formdan gelen veriyi Core.API'ye gönderir (POST)
    [HttpPost]
    public async Task<IActionResult> Create(TrafficFineViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var client = _httpClientFactory.CreateClient("CoreApi");
        
        // YENİ EKLENEN KISIM: Token'ı Cookie'den alıp isteğin başlığına (Header) ekliyoruz
        var token = Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(token))
        {
            // "Buyur memur bey, kimliğim (Token'ım) burada" diyoruz
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            // Eğer token yoksa veya süresi dolmuşsa uyar
            ModelState.AddModelError(string.Empty, "Oturumunuz bulunamadı. Lütfen tekrar giriş yapın.");
            return View(model);
        }

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(model), 
            System.Text.Encoding.UTF8, 
            "application/json");

        var response = await client.PostAsync("/api/Fines", jsonContent);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("Index");
        }

        var error = await response.Content.ReadAsStringAsync();
        ModelState.AddModelError(string.Empty, $"Ceza eklenirken API hatası: {error}");
        
        return View(model);
    }
    
    
    // Cezanın detaylarını getirir (GET)
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var client = _httpClientFactory.CreateClient("CoreApi");
        
        // YENİ EKLENEN KISIM: Kimliğimizi (Token) bu isteğe de ekliyoruz
        var token = Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.GetAsync($"/api/Fines/{id}");

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var fine = JsonSerializer.Deserialize<TrafficFineViewModel>(jsonString, jsonOptions);
            
            return View(fine);
        }

        // HATA YAKALAYICIYI AKTİF ETTİK:
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"API'den Hata Geldi: {response.StatusCode} - {error}");
        
    }
    

    // Cezanın durumunu günceller (POST)
    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromForm] int id, [FromForm] int newStatus)
    {
        var client = _httpClientFactory.CreateClient("CoreApi");
        
        var token = Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var payload = new { NewStatus = newStatus, Reason = "Durum web arayüzünden güncellendi (AJAX)." };
        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        
        var response = await client.PutAsync($"/api/Fines/{id}/status", jsonContent);

        if (response.IsSuccessStatusCode)
        {
            // İşlem başarılıysa arayüze (JS'e) "Tamamdır" mesajı gönderiyoruz
            return Json(new { success = true });
        }

        return Json(new { success = false, message = "Güncelleme sırasında bir hata oluştu." });
    }
    
    
}