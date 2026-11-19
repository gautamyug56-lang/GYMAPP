using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Rackroom.Web.Pages.Admin;

public class TestModel : PageModel
{
    public Guid? GymId { get; private set; }

    public void OnGet()
    {
        string? value = User.FindFirstValue("GymId");
        if (Guid.TryParse(value, out Guid gymId))
            GymId = gymId;
    }
}
