using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;

namespace Rackroom.Web.Pages.Payments;

[Authorize(Policy = "StaffOrAdmin")]
public class InvoiceModel : PageModel
{
    private readonly ICurrentGymContext _currentGymContext;
    private readonly IInvoicePdfStorage _storage;

    public InvoiceModel(
        ICurrentGymContext currentGymContext,
        IInvoicePdfStorage storage)
    {
        _currentGymContext = currentGymContext;
        _storage = storage;
    }

    public async Task<IActionResult> OnGetAsync(
        int invoiceNumber,
        CancellationToken cancellationToken)
    {
        Guid gymId = _currentGymContext.GymId
            ?? throw new BusinessRuleException("No gym is associated with the current user.");

        if (invoiceNumber <= 0)
            return NotFound();

        byte[]? pdf = await _storage.ReadAsync(
            gymId,
            invoiceNumber,
            cancellationToken);

        return pdf is null
            ? NotFound()
            : File(pdf, "application/pdf", $"invoice-{invoiceNumber}.pdf");
    }
}
