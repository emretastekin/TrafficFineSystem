using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrafficFineSystem.Audit.API.Data;

namespace TrafficFineSystem.Audit.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HistoriesController : ControllerBase
{
    private readonly AuditDbContext _context;

    public HistoriesController(AuditDbContext context)
    {
        _context = context;
    }

    // Belirli bir cezaya ait tüm işlem geçmişini tarihe göre yeniden eskiye getirir
    [HttpGet("fine/{fineId}")]
    public async Task<IActionResult> GetFineHistories(int fineId)
    {
        var histories = await _context.FineHistories
            .Where(h => h.TrafficFineId == fineId)
            .OrderByDescending(h => h.ProcessDate)
            .ToListAsync();
            
        return Ok(histories);
    }
}