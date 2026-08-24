using Microsoft.AspNetCore.Mvc;
using TrafficFineSystem.Core.API.Repositories.Interfaces;
using TrafficFineSystem.Shared.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrafficFineSystem.Core.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDistributedCache _cache;

    public VehiclesController(IVehicleRepository vehicleRepository, IDistributedCache cache)
    {
        _vehicleRepository = vehicleRepository;
        _cache = cache;
    }

    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        string cacheKey = "vehiclesList";
        string? cachedVehicles = await _cache.GetStringAsync(cacheKey);

        var jsonOptions = new JsonSerializerOptions 
        { 
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNameCaseInsensitive = true
        };

        if (!string.IsNullOrEmpty(cachedVehicles))
        {
            // Veri Redis'te (Cache'de) varsa, hızlıca dön!
            var vehiclesFromCache = JsonSerializer.Deserialize<IEnumerable<Vehicle>>(cachedVehicles, jsonOptions);
            return Ok(vehiclesFromCache);
        }

        // Veri Redis'te yoksa Veritabanından al
        var vehicles = await _vehicleRepository.GetAllAsync();

        // Araçlar sık değişmediği için 30 dakika önbellekte tutuyoruz
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        string serializedVehicles = JsonSerializer.Serialize(vehicles, jsonOptions);
        await _cache.SetStringAsync(cacheKey, serializedVehicles, cacheOptions);

        return Ok(vehicles);
    }
    

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(id);
        if (vehicle == null) return NotFound("Araç bulunamadı.");
        return Ok(vehicle);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Vehicle vehicle)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var createdVehicle = await _vehicleRepository.AddAsync(vehicle);
        
        await _cache.RemoveAsync("vehiclesList");
        
        return CreatedAtAction(nameof(GetById), new { id = createdVehicle.Id }, createdVehicle);
    }
}