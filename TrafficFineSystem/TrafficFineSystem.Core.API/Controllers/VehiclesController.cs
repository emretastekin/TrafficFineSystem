using Microsoft.AspNetCore.Mvc;
using TrafficFineSystem.Core.API.Repositories.Interfaces;
using TrafficFineSystem.Shared.Entities;

namespace TrafficFineSystem.Core.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleRepository _vehicleRepository;

    public VehiclesController(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var vehicles = await _vehicleRepository.GetAllAsync();
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
        return CreatedAtAction(nameof(GetById), new { id = createdVehicle.Id }, createdVehicle);
    }
}