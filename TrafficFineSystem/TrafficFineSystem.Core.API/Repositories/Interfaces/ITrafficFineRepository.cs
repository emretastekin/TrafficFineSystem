using TrafficFineSystem.Shared.Entities;

namespace TrafficFineSystem.Core.API.Repositories.Interfaces;

public interface ITrafficFineRepository
{
    Task<IEnumerable<TrafficFine>> GetAllAsync();
    Task<TrafficFine?> GetByIdAsync(int id);
    Task<IEnumerable<TrafficFine>> GetByVehicleIdAsync(int vehicleId);
    Task<TrafficFine> AddAsync(TrafficFine trafficFine);
    Task UpdateAsync(TrafficFine trafficFine);
}