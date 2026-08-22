using TrafficFineSystem.Shared.Entities;

namespace TrafficFineSystem.Core.API.Repositories.Interfaces;

public interface IVehicleRepository
{
    Task<IEnumerable<Vehicle>> GetAllAsync();
    Task<Vehicle?> GetByIdAsync(int id);
    Task<Vehicle> AddAsync(Vehicle vehicle);
}