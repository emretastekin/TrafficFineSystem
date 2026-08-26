using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TrafficFineSystem.WebApp.Models;

namespace TrafficFineSystem.WebApp.Controllers;

public class VehiclesController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public VehiclesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // Araçları Listele (GET)
    public async Task<IActionResult> Index()
    {
        var client = _httpClientFactory.CreateClient("CoreApi");
        var response = await client.GetAsync("/api/Vehicles");

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var vehicles = JsonSerializer.Deserialize<List<VehicleViewModel>>(jsonString, options);
            
            return View(vehicles);
        }

        return View(new List<VehicleViewModel>());
    }

    // Yeni Araç Ekleme Sayfası (GET)
    [HttpGet]
    public IActionResult Create()
    {
        return View(new VehicleViewModel());
    }

    // Yeni Araç Kaydetme (POST)
    [HttpPost]
    public async Task<IActionResult> Create(VehicleViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var client = _httpClientFactory.CreateClient("CoreApi");
        
        // Yetki gerektiriyorsa Token'ı ekliyoruz
        var token = Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var jsonContent = new StringContent(JsonSerializer.Serialize(model), System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/Vehicles", jsonContent);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("Index");
        }

        var error = await response.Content.ReadAsStringAsync();
        ModelState.AddModelError(string.Empty, $"Araç eklenirken hata oluştu: {error}");
        return View(model);
    }
}