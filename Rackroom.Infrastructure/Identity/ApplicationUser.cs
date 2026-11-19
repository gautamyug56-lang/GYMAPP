using Microsoft.AspNetCore.Identity;

namespace Rackroom.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid GymId { get; set; }

    // Member users are linked to the domain Member record.
    // Staff and Admin users leave this null.
    public Guid? MemberId { get; set; }
}
