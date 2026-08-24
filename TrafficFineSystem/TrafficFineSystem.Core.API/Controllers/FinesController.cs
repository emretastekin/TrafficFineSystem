using Microsoft.AspNetCore.Mvc;
using TrafficFineSystem.Core.API.Repositories.Interfaces;
using TrafficFineSystem.Core.API.Services;
using TrafficFineSystem.Shared.Entities;
using TrafficFineSystem.Shared.Enums;
using TrafficFineSystem.Shared.Events;

namespace TrafficFineSystem.Core.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FinesController : ControllerBase
{
    private readonly ITrafficFineRepository _fineRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly KafkaProducerService _kafkaProducerService; // Kafka servisini enjekte ediyoruz

    public FinesController(
        ITrafficFineRepository fineRepository, 
        IVehicleRepository vehicleRepository,
        KafkaProducerService kafkaProducerService)
    {
        _fineRepository = fineRepository;
        _vehicleRepository = vehicleRepository;
        _kafkaProducerService = kafkaProducerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var fines = await _fineRepository.GetAllAsync();
        return Ok(fines);
    }

    [HttpGet("vehicle/{vehicleId}")]
    public async Task<IActionResult> GetByVehicle(int vehicleId)
    {
        var fines = await _fineRepository.GetByVehicleIdAsync(vehicleId);
        return Ok(fines);
    }

    [HttpPost]
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

        return Ok(createdFine);
    }
}