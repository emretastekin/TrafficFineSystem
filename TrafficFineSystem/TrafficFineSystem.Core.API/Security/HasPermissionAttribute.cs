using Microsoft.AspNetCore.Authorization;

namespace TrafficFineSystem.Core.API.Security;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base(policy: permission)
    {
    }
}