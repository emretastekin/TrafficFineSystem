using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrafficFineSystem.Core.API.Data;

namespace TrafficFineSystem.Core.API.Security;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public PermissionAuthorizationHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // Firebase, User ID'yi "user_id" veya "NameIdentifier" claim'i içinde gönderir
        var userId = context.User.FindFirst(c => c.Type == "user_id" || c.Type == ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return; // Yetkisiz (Kullanıcı giriş yapmamış)
        }

        // DbContext'i scope içinden çağırıyoruz (Mimari bir best-practice)
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Veritabanında kullanıcının bu yetkiye sahip olup olmadığını sorguluyoruz
        var hasPermission = await dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .AnyAsync(p => p == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement); // Yetki onaylandı!
        }
    }
}