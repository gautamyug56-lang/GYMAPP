using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Payments;

[Authorize(Policy = "StaffOrAdmin")]
public class IndexModel : PageModel
{
    private readonly IPaymentManagementService _paymentService;

    public IndexModel(IPaymentManagementService paymentService)
    {
        _paymentService = paymentService;
    }

    public IReadOnlyList<Payment> Payments { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Payments = await _paymentService.GetPaymentsAsync(cancellationToken);
    }
}
