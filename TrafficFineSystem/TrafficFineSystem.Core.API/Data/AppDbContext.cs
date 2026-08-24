using Microsoft.EntityFrameworkCore;
using TrafficFineSystem.Shared.Entities;

namespace TrafficFineSystem.Core.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<TrafficFine> Fines { get; set; }
    public DbSet<FineHistory> FineHistories { get; set; }
    
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Tutar (Amount) alanı için küsurat hassasiyetini belirliyoruz (örn: 1500.50)
        modelBuilder.Entity<TrafficFine>()
            .Property(f => f.Amount)
            .HasColumnType("decimal(18,2)");
        
        // RolePermission (Çoka-Çok İlişki Anahtarları)
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // UserRole (Kullanıcı ve Rol İlişki Anahtarları)
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });
    }
}