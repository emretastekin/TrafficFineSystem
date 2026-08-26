using Microsoft.AspNetCore.Mvc;
using TrafficFineSystem.Core.API.Repositories.Interfaces;
using TrafficFineSystem.Shared.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;
using TrafficFineSystem.Core.API.Services;
using TrafficFineSystem.Shared.Events;

namespace TrafficFineSystem.Core.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDistributedCache _cache;
    private readonly KafkaProducerService _kafkaProducerService;

    public VehiclesController(IVehicleRepository vehicleRepository, IDistributedCache cache, KafkaProducerService kafkaProducerService)
    {
        _vehicleRepository = vehicleRepository;
        _cache = cache;
        _kafkaProducerService = kafkaProducerService;
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
        
        var vehicleEvent = new VehicleCreatedEvent 
        {
            VehicleId = createdVehicle.Id,
            Plate = createdVehicle.Plate,
            ProcessDate = DateTime.UtcNow
        };
        
        await _kafkaProducerService.ProduceVehicleCreatedEventAsync(vehicleEvent);
        
        await _cache.RemoveAsync("vehiclesList");
        
        return CreatedAtAction(nameof(GetById), new { id = createdVehicle.Id }, createdVehicle);
    }
}