using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Rackroom.Application.Abstractions;

namespace Rackroom.Infrastructure.Tenant;

public class CurrentGymContext : ICurrentGymContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentGymContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GymId
    {
        get
        {
            string? value = _httpContextAccessor.HttpContext?
                .User.FindFirstValue("GymId");

            return Guid.TryParse(value, out Guid gymId)
                ? gymId
                : null;
        }
    }
}
