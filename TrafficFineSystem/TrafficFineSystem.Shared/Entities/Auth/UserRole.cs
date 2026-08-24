namespace TrafficFineSystem.Shared.Entities;

public class UserRole
{
    // Firebase User ID'leri string (GUID veya alfanumerik) olduğu için UserId string tanımlanmalıdır!
    public string UserId { get; set; } = string.Empty; 
    
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}