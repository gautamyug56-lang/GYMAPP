using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;

namespace Rackroom.Web.Pages.Payments;

[Authorize(Policy = "StaffOrAdminOrMember")]
public class InvoiceModel : PageModel
{
    private readonly ICurrentGymContext _currentGymContext;
    private readonly IInvoicePdfStorage _storage;
    private readonly IRackroomDataStore _dataStore;

    public InvoiceModel(
        ICurrentGymContext currentGymContext,
        IInvoicePdfStorage storage,
        IRackroomDataStore dataStore)
    {
        _currentGymContext = currentGymContext;
        _storage = storage;
        _dataStore = dataStore;
    }

    public async Task<IActionResult> OnGetAsync(
        int invoiceNumber,
        CancellationToken cancellationToken)
    {
        Guid gymId = _currentGymContext.GymId
            ?? throw new BusinessRuleException("No gym is associated with the current user.");

        if (invoiceNumber <= 0)
            return NotFound();

        if (User.IsInRole("Member"))
        {
            Guid memberId = _currentGymContext.MemberId ?? Guid.Empty;
            if (memberId == Guid.Empty ||
                !await _dataStore.InvoiceBelongsToMemberAsync(gymId, invoiceNumber, memberId, cancellationToken))
            {
                return Forbid();
            }
        }

        byte[]? pdf = await _storage.ReadAsync(
            gymId,
            invoiceNumber,
            cancellationToken);

        return pdf is null
            ? NotFound()
            : File(pdf, "application/pdf", $"invoice-{invoiceNumber}.pdf");
    }
}
