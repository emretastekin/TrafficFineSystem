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
        var token = Request.Cookies["AuthToken"];
        
        // 1. KONTROL: Eğer kullanıcının token'ı (kimliği) hiç yoksa direkt giriş sayfasına yolla
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login", "Auth");
        }

        var client = _httpClientFactory.CreateClient("CoreApi");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var response = await client.GetAsync("/api/Fines");

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var fines = JsonSerializer.Deserialize<List<TrafficFineViewModel>>(jsonString, jsonOptions);
            return View(fines);
        }

        // 2. KONTROL: Token var ama API 401 (Yetkisiz) veya 403 (Yasak) döndüyse (Örn: Süresi dolmuşsa)
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            Response.Cookies.Delete("AuthToken"); // Geçersiz token'ı tarayıcıdan sil
            return RedirectToAction("Login", "Auth"); // Giriş sayfasına yolla
        }

        // Eğer sunucu çöktüyse vb. başka bir hataysa o zaman hatayı göster
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
        var token = Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        // 1. Core.API'den ceza detayını çekiyoruz
        var response = await client.GetAsync($"/api/Fines/{id}");
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Ceza detayları alınamadı.");
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var fine = JsonSerializer.Deserialize<TrafficFineViewModel>(jsonString, jsonOptions);

        // 2. YENİ EKLENEN: Audit.API'den işlem geçmişini çekiyoruz 
        // (Audit.API portunun 5297 olduğunu Layout dosyasındaki SignalR bağlantısından biliyoruz)
        using var auditClient = new HttpClient();
        var historyResponse = await auditClient.GetAsync($"http://localhost:5297/api/Histories/fine/{id}");
        
        if (historyResponse.IsSuccessStatusCode)
        {
            var historyJson = await historyResponse.Content.ReadAsStringAsync();
            var histories = JsonSerializer.Deserialize<List<FineHistoryViewModel>>(historyJson, jsonOptions);
            
            // Geçmiş listesini ViewBag ile arayüze (View'a) aktarıyoruz
            ViewBag.Histories = histories;
        }
        else
        {
            ViewBag.Histories = new List<FineHistoryViewModel>(); // Hata olursa boş liste gitsin
        }
        
        
        // YENİ EKLENEN: Core.API'ye giriş yapan kişinin veritabanında Admin olup olmadığını soruyoruz
        var adminCheckResponse = await client.GetAsync("/api/Fines/is-admin");
        if (adminCheckResponse.IsSuccessStatusCode)
        {
            var isAdminStr = await adminCheckResponse.Content.ReadAsStringAsync();
            ViewBag.IsAdmin = bool.Parse(isAdminStr); // true veya false olarak ViewBag'e atıyoruz
        }
        else
        {
            ViewBag.IsAdmin = false;
        }
        

        return View(fine);
    }
    
    

    // Cezanın durumunu günceller (POST)
    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromForm] int id, [FromForm] int newStatus, [FromForm] string reason = "")
    {
        var client = _httpClientFactory.CreateClient("CoreApi");
        
        var token = Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        // Eğer işlem iptal (3) ise kullanıcının yazdığı nedeni, ödeme (2) ise standart bir metni kullanıyoruz.
        string finalReason = newStatus == 3 ? reason : "Ceza tahsil edildi.";

        var payload = new { NewStatus = newStatus, Reason = finalReason };
        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
        
        var response = await client.PutAsync($"/api/Fines/{id}/status", jsonContent);

        if (response.IsSuccessStatusCode)
        {
            return Json(new { success = true });
        }

        var errorMessage = await response.Content.ReadAsStringAsync();
        return Json(new { success = false, message = $"Güncelleme başarısız: {errorMessage}" });
    }
    
    
}