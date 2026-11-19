using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Payments;

[Authorize(Policy = "StaffOrAdmin")]
public class CreateModel : PageModel
{
    private readonly IMembershipManagementService _membershipService;
    private readonly IPaymentManagementService _paymentService;

    public CreateModel(
        IMembershipManagementService membershipService,
        IPaymentManagementService paymentService)
    {
        _membershipService = membershipService;
        _paymentService = paymentService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Membership> Memberships { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadMembershipsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadMembershipsAsync(cancellationToken);

        if (!ModelState.IsValid)
            return Page();

        try
        {
            PaymentReceipt receipt = await _paymentService.RecordPaymentAsync(
                Input.MembershipId,
                Input.Amount,
                DateTime.SpecifyKind(Input.PaidAtUtc.Date, DateTimeKind.Utc),
                Input.Method,
                Input.Reference,
                Input.Notes,
                cancellationToken);

            return RedirectToPage(
                "./Invoice",
                new { invoiceNumber = receipt.Invoice.InvoiceNumber });
        }
        catch (BusinessRuleException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    private async Task LoadMembershipsAsync(CancellationToken cancellationToken)
    {
        Memberships = (await _membershipService.GetMembershipsAsync(cancellationToken))
            .Where(x => x.Status != MembershipStatus.Cancelled &&
                        x.Member?.IsActive == true)
            .OrderBy(x => x.Member!.FirstName)
            .ThenBy(x => x.Member!.LastName)
            .ToList();
    }

    public sealed class InputModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public Guid MembershipId { get; set; }

        [System.ComponentModel.DataAnnotations.Range(
            typeof(decimal), "0.01", "100000000")]
        public decimal Amount { get; set; }

        public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow.Date;

        [System.ComponentModel.DataAnnotations.Required]
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

        [System.ComponentModel.DataAnnotations.StringLength(200)]
        public string? Reference { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string? Notes { get; set; }
    }
}
