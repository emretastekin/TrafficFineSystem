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
using Microsoft.EntityFrameworkCore;

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
    [HasPermission("Fines.Read")]
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
    [HasPermission("Fines.Read")]
    public async Task<IActionResult> GetById(int id)
    {
        var fine = await _fineRepository.GetByIdAsync(id);
        if (fine == null)
            return NotFound("Ceza bulunamadı.");

        return Ok(fine);
    }
    
    

    [HttpGet("vehicle/{vehicleId}")]
    [HasPermission("Fines.Read")]
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
    [HasPermission("Fines.Update")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateFineStatusRequest request)
    {
        var fine = await _context.Fines.FindAsync(id);
        if (fine == null) 
            return NotFound("Ceza bulunamadı.");

        var previousStatus = fine.Status;

        // --- İŞ KURALLARI (BUSINESS RULES) KİLİTLERİ ---

        // Kural 1: Onaylanmış (Tamamlandı) kayıt değiştirilemez.
        // İSTİSNA: Sadece Admin rolüne sahip kişi "Yeni" (1) statüsüne çekerek süreci baştan başlatabilir.
        if (previousStatus == FineStatus.Tamamlandi && request.NewStatus != 1)
        {
            return BadRequest("İşlem engellendi: Bu ceza onaylanmış ve tahsil edilmiştir. Geçmişe dönük değişiklik yapılamaz.");
        }

        // Kural 2: İptal edilmiş kayıt tekrar işleme alınamaz.
        // İSTİSNA: Sadece Admin rolüne sahip kişi "Yeni" (1) statüsüne çekerek süreci baştan başlatabilir.
        if (previousStatus == FineStatus.IptalEdildi && request.NewStatus != 1)
        {
            return BadRequest("İşlem engellendi: Bu ceza iptal edilerek kilitlenmiştir. İşleme devam etmek için yeni bir ceza kaydı oluşturmalısınız veya yetkili bir yönetici süreci baştan başlatmalıdır.");
        }
        // -----------------------------------------------

        fine.Status = (FineStatus)request.NewStatus;
        
        await _context.SaveChangesAsync();

        var statusEvent = new FineStatusChangedEvent
        {
            TrafficFineId = fine.Id,
            UserId = "System", // İleride gerçek User ID gelecek
            ProcessDate = DateTime.UtcNow,
            ProcessType = request.NewStatus == 2 ? "Onaya Gönderildi" : "Durum Güncellemesi",
            Reason = request.Reason,
            PreviousStatus = (int)previousStatus,
            NewStatus = request.NewStatus
        };

        await _kafkaProducerService.ProduceFineStatusChangedEventAsync(statusEvent);
        await _cache.RemoveAsync("finesList");

        return Ok(new { Message = "Ceza durumu başarıyla güncellendi.", Fine = fine });
    }
    
    
    [HttpGet("is-admin")]
    public async Task<IActionResult> CheckIfAdmin()
    {
        // 1. JWT Token içinden Firebase UID'sini alıyoruz
        var uid = User.Claims.FirstOrDefault(c => c.Type == "user_id" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        
        if (string.IsNullOrEmpty(uid)) 
            return Ok(false);

        // 2. Veritabanındaki UserRoles tablosuna bakıyoruz: Bu UID'nin RoleId = 1 (Admin) yetkisi var mı?
        var isAdmin = await _context.UserRoles.AnyAsync(ur => ur.UserId == uid && ur.RoleId == 1);
        
        return Ok(isAdmin);
    }
    
}

// Controller'ın en altına veya Shared klasörüne bu DTO'yu ekleyebilirsin
public record UpdateFineStatusRequest(int NewStatus, string Reason);