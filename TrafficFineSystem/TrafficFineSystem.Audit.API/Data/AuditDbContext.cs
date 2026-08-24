using Microsoft.EntityFrameworkCore;
using TrafficFineSystem.Shared.Entities;

namespace TrafficFineSystem.Audit.API.Data;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    public DbSet<FineHistory> FineHistories { get; set; }
}