using Microsoft.AspNetCore.Identity;

namespace Rackroom.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid GymId { get; set; }
}
