using Microsoft.AspNetCore.Mvc;
using TrafficFineSystem.Core.API.Data;
using TrafficFineSystem.Core.API.Repositories.Interfaces;
using TrafficFineSystem.Core.API.Security;
using TrafficFineSystem.Core.API.Services;
using TrafficFineSystem.Shared.Entities;
using TrafficFineSystem.Shared.Enums;
using TrafficFineSystem.Shared.Events;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace TrafficFineSystem.Core.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FinesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITrafficFineRepository _fineRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly KafkaProducerService _kafkaProducerService; // Kafka servisini enjekte ediyoruz
    private readonly IDistributedCache _cache;
    

    public FinesController(
        ITrafficFineRepository fineRepository, 
        IVehicleRepository vehicleRepository,
        KafkaProducerService kafkaProducerService,
        AppDbContext context,
        IDistributedCache cache)
    {
        _fineRepository = fineRepository;
        _vehicleRepository = vehicleRepository;
        _kafkaProducerService = kafkaProducerService;
        _context = context;
        _cache = cache;
    }

    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        string cacheKey = "finesList";
        string? cachedFines = await _cache.GetStringAsync(cacheKey);

        // YENİ EKLENEN KISIM: Redis çevirileri için döngü yoksayma ayarı
        var jsonOptions = new JsonSerializerOptions 
        { 
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            PropertyNameCaseInsensitive = true
        };

        if (!string.IsNullOrEmpty(cachedFines))
        {
            // Veri Cache'de varsa, okurken de bu ayarı kullan
            var finesFromCache = JsonSerializer.Deserialize<IEnumerable<TrafficFine>>(cachedFines, jsonOptions);
            return Ok(finesFromCache);
        }

        var fines = await _fineRepository.GetAllAsync();

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        // YENİ EKLENEN KISIM: Cache'e yazarken döngü yoksayma ayarını kullan
        string serializedFines = JsonSerializer.Serialize(fines, jsonOptions);
        await _cache.SetStringAsync(cacheKey, serializedFines, cacheOptions);

        return Ok(fines);
    }
    
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var fine = await _fineRepository.GetByIdAsync(id);
        if (fine == null)
            return NotFound("Ceza bulunamadı.");

        return Ok(fine);
    }
    
    

    [HttpGet("vehicle/{vehicleId}")]
    public async Task<IActionResult> GetByVehicle(int vehicleId)
    {
        var fines = await _fineRepository.GetByVehicleIdAsync(vehicleId);
        return Ok(fines);
    }

    [HttpPost]
    [HasPermission("Fines.Create")]
    public async Task<IActionResult> Create(TrafficFine fine)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var vehicleExists = await _vehicleRepository.GetByIdAsync(fine.VehicleId);
        if (vehicleExists == null) return BadRequest("Belirtilen araç sistemde bulunamadı.");

        fine.Status = FineStatus.Yeni;
        fine.IssueDate = DateTime.UtcNow;

        var createdFine = await _fineRepository.AddAsync(fine);

        // Yeni ceza oluşturulduğunda Kafka'ya bir event fırlatıyoruz
        var statusEvent = new FineStatusChangedEvent
        {
            TrafficFineId = createdFine.Id,
            UserId = "System",
            ProcessDate = DateTime.UtcNow,
            ProcessType = "Yeni", // DİKKAT: ProcessType eğer veritabanında 'string' ise burayı "1" veya "Yeni" yap. Eğer int ise 1 kalsın. (FineHistory.cs entity'sine bakarak buna karar vermelisin).
            Reason = "Yeni Ceza Oluşturuldu", // Açıklamayı buraya aldım
            PreviousStatus = (int)TrafficFineSystem.Shared.Enums.FineStatus.Yeni, 
            NewStatus = (int)TrafficFineSystem.Shared.Enums.FineStatus.Yeni
        };

        await _kafkaProducerService.ProduceFineStatusChangedEventAsync(statusEvent);
        
        await _cache.RemoveAsync("finesList");

        return Ok(createdFine);
    }
    
    
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateFineStatusRequest request)
    {
        var fine = await _context.Fines.FindAsync(id);
        if (fine == null) 
            return NotFound("Ceza bulunamadı.");

        var previousStatus = fine.Status;
        fine.Status = (FineStatus)request.NewStatus;
        
        // Önce kendi veritabanımızda cezayı güncelliyoruz
        await _context.SaveChangesAsync();

        // Kafka için event modelimizi oluşturuyoruz
        var statusEvent = new FineStatusChangedEvent
        {
            TrafficFineId = fine.Id,
            UserId = "System", // İleride Firebase entegrasyonuyla buraya gerçek User ID gelecek
            ProcessDate = DateTime.UtcNow,
            ProcessType = "Güncelleme",
            Reason = request.Reason,
            PreviousStatus = (int)previousStatus,
            NewStatus = request.NewStatus
        };

        // Mesajı Kafka'ya fırlatıyoruz
        await _kafkaProducerService.ProduceFineStatusChangedEventAsync(statusEvent);
        
        await _cache.RemoveAsync("finesList");

        return Ok(new { Message = "Ceza durumu başarıyla güncellendi ve loglandı.", Fine = fine });
    }
    
}

// Controller'ın en altına veya Shared klasörüne bu DTO'yu ekleyebilirsin
public record UpdateFineStatusRequest(int NewStatus, string Reason);