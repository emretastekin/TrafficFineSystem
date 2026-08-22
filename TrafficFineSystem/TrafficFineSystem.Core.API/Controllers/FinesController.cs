using Microsoft.AspNetCore.Mvc;
using TrafficFineSystem.Core.API.Repositories.Interfaces;
using TrafficFineSystem.Shared.Entities;
using TrafficFineSystem.Shared.Enums;

namespace TrafficFineSystem.Core.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FinesController : ControllerBase
{
    private readonly ITrafficFineRepository _fineRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public FinesController(ITrafficFineRepository fineRepository, IVehicleRepository vehicleRepository)
    {
        _fineRepository = fineRepository;
        _vehicleRepository = vehicleRepository;
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

        // Aracın var olup olmadığını kontrol ediyoruz
        var vehicleExists = await _vehicleRepository.GetByIdAsync(fine.VehicleId);
        if (vehicleExists == null) return BadRequest("Belirtilen araç sistemde bulunamadı.");

        // Yeni ceza oluşturulurken statüsü her zaman 'Yeni' olmalı
        fine.Status = FineStatus.Yeni;
        fine.IssueDate = DateTime.UtcNow;

        var createdFine = await _fineRepository.AddAsync(fine);
        return Ok(createdFine);
    }
}