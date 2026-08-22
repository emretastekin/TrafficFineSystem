using Microsoft.EntityFrameworkCore;
using TrafficFineSystem.Core.API.Data;
using TrafficFineSystem.Core.API.Repositories.Interfaces;
using TrafficFineSystem.Shared.Entities;

namespace TrafficFineSystem.Core.API.Repositories;

public class TrafficFineRepository : ITrafficFineRepository
{
    private readonly AppDbContext _context;

    public TrafficFineRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TrafficFine>> GetAllAsync()
    {
        return await _context.Fines.Include(f => f.Vehicle).ToListAsync();
    }

    public async Task<TrafficFine?> GetByIdAsync(int id)
    {
        return await _context.Fines.Include(f => f.Vehicle).FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<TrafficFine>> GetByVehicleIdAsync(int vehicleId)
    {
        return await _context.Fines.Where(f => f.VehicleId == vehicleId).ToListAsync();
    }

    public async Task<TrafficFine> AddAsync(TrafficFine trafficFine)
    {
        _context.Fines.Add(trafficFine);
        await _context.SaveChangesAsync();
        return trafficFine;
    }

    public async Task UpdateAsync(TrafficFine trafficFine)
    {
        _context.Fines.Update(trafficFine);
        await _context.SaveChangesAsync();
    }
}