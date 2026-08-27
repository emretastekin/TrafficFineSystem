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
    // [HasPermission("Fines.Update")] 
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateFineStatusRequest request)
    {
        var fine = await _context.Fines.FindAsync(id);
        if (fine == null) 
            return NotFound("Ceza bulunamadı.");

        var previousStatus = fine.Status;

        // 1. Kullanıcının kimliğini al ve Admin olup olmadığını kontrol et
        var uid = User.Claims.FirstOrDefault(c => c.Type == "user_id" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        bool isAdmin = await _context.UserRoles.AnyAsync(ur => ur.UserId == uid && ur.RoleId == 1);

        // --- İŞ KURALLARI (BUSINESS RULES) ---
        
        // YENİ KURAL 3: HİYERARŞİK ONAY SÜRECİ
        if (!isAdmin)
        {
            // Eğer giriş yapan kişi Admin değilse (Memur ise), SADECE "Yeni" (1) durumunu "Onay Bekliyor" (2) yapabilir!
            if (previousStatus != FineStatus.Yeni || request.NewStatus != 2)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Yetki Hatası: Memurlar sadece yeni cezaları onaya gönderebilir. Onaylama veya İptal işlemleri Yönetici yetkisi gerektirir.");
            }
        }

        // Kural 1: Onaylanmış (Tamamlandı) kayıt değiştirilemez.
        if (previousStatus == FineStatus.Tamamlandi && request.NewStatus != 1)
        {
            return BadRequest("İşlem engellendi: Bu ceza onaylanmış ve tahsil edilmiştir. Geçmişe dönük değişiklik yapılamaz.");
        }

        // Kural 2: İptal edilmiş kayıt tekrar işleme alınamaz.
        if (previousStatus == FineStatus.IptalEdildi && request.NewStatus != 1)
        {
            return BadRequest("İşlem engellendi: Bu ceza iptal edilerek kilitlenmiştir.");
        }
        // -------------------------------------

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
    
    
    // Dashboard için özet istatistikleri getirir
    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var fines = await _context.Fines.ToListAsync();

        var totalPaidAmount = fines.Where(f => f.Status == FineStatus.Tamamlandi).Sum(f => f.Amount);
        
        // YENİ EKLENEN: Tahsil edilen ceza adeti
        var paidCount = fines.Count(f => f.Status == FineStatus.Tamamlandi); 
        
        var pendingCount = fines.Count(f => f.Status == FineStatus.OnayBekliyor);
        var newCount = fines.Count(f => f.Status == FineStatus.Yeni);
        var canceledCount = fines.Count(f => f.Status == FineStatus.IptalEdildi);
        // YENİ EKLENEN: Veritabanındaki toplam araç sayısını çekiyoruz
        var totalVehicles = await _context.Vehicles.CountAsync();
        
        // YENİ EKLENEN: Araçları "Tiplerine" göre gruplayıp sayıyoruz (Binek: 5, Çekici: 2 vb.)
        var vehicles = await _context.Vehicles.ToListAsync();
        var vehicleTypeStats = vehicles.GroupBy(v => v.Type)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        return Ok(new
        {
            TotalPaidAmount = totalPaidAmount,
            PaidFinesCount = paidCount, // YENİ EKLENEN
            PendingApprovals = pendingCount,
            NewFines = newCount,
            CanceledFines = canceledCount,
            TotalVehicles = totalVehicles,
            VehicleTypeStats = vehicleTypeStats
        });
    }
    
}

// Controller'ın en altına veya Shared klasörüne bu DTO'yu ekleyebilirsin
public record UpdateFineStatusRequest(int NewStatus, string Reason);