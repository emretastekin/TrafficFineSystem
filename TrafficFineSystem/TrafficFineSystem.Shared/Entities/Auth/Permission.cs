namespace TrafficFineSystem.Shared.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Örn: "Fines.Read", "Fines.Create", "Fines.Update"
    
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}