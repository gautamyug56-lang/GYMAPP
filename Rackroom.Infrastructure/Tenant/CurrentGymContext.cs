using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Rackroom.Application.Abstractions;

namespace Rackroom.Infrastructure.Tenant;

public sealed class CurrentGymContext : ICurrentGymContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentGymContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GymId => ReadGuidClaim("GymId");
    public Guid? MemberId => ReadGuidClaim("MemberId");

    private Guid? ReadGuidClaim(string type)
    {
        string? value = _httpContextAccessor.HttpContext?.User.FindFirstValue(type);
        return Guid.TryParse(value, out Guid id) ? id : null;
    }
}
