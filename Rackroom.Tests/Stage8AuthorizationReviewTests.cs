using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Web.Pages.Admin;
using ChurnPages = Rackroom.Web.Pages.Admin.Churn;
using AttendancePages = Rackroom.Web.Pages.Attendance;
using MemberPortalPages = Rackroom.Web.Pages.MemberPortal;
using MemberPages = Rackroom.Web.Pages.Members;
using MembershipPages = Rackroom.Web.Pages.Memberships;
using PaymentPages = Rackroom.Web.Pages.Payments;
using PlanPages = Rackroom.Web.Pages.Plans;
using WorkoutPlanPages = Rackroom.Web.Pages.WorkoutPlans;
using PaymentPages = Rackroom.Web.Pages.Payments;

namespace Rackroom.Tests;

public class Stage8AuthorizationReviewTests
{
    [Fact]
    public void Admin_pages_require_admin_policy()
    {
        AssertPolicy<DashboardModel>("AdminOnly");
        AssertPolicy<ChurnPages.IndexModel>("AdminOnly");
        AssertPolicy<ChurnPages.TrainModel>("AdminOnly");
    }

    [Fact]
    public void Member_portal_pages_require_member_policy()
    {
        AssertPolicy<MemberPortalPages.IndexModel>("MemberOnly");
        AssertPolicy<MemberPortalPages.PauseModel>("MemberOnly");
        AssertPolicy<MemberPortalPages.CancelModel>("MemberOnly");
        AssertPolicy<MemberPortalPages.MeasurementModel>("MemberOnly");
    }

    [Fact]
    public void Staff_pages_require_staff_or_admin_policy()
    {
        AssertPolicy<MemberPages.IndexModel>("StaffOrAdmin");
        AssertPolicy<MemberPages.CreateModel>("StaffOrAdmin");
        AssertPolicy<MemberPages.EditModel>("StaffOrAdmin");
        AssertPolicy<MemberPages.DeactivateModel>("StaffOrAdmin");
        AssertPolicy<MembershipPages.IndexModel>("StaffOrAdmin");
        AssertPolicy<MembershipPages.CreateModel>("StaffOrAdmin");
        AssertPolicy<MembershipPages.PauseModel>("StaffOrAdmin");
        AssertPolicy<MembershipPages.ApprovePauseModel>("StaffOrAdmin");
        AssertPolicy<MembershipPages.CancelModel>("StaffOrAdmin");
        AssertPolicy<PlanPages.IndexModel>("StaffOrAdmin");
        AssertPolicy<PlanPages.CreateModel>("StaffOrAdmin");
        AssertPolicy<PaymentPages.IndexModel>("StaffOrAdmin");
        AssertPolicy<PaymentPages.CreateModel>("StaffOrAdmin");
        AssertPolicy<AttendancePages.DeskModel>("StaffOrAdmin");
        AssertPolicy<AttendancePages.StaffCheckInModel>("StaffOrAdmin");
        AssertPolicy<WorkoutPlanPages.IndexModel>("StaffOrAdmin");
        AssertPolicy<WorkoutPlanPages.CreateModel>("StaffOrAdmin");
        AssertPolicy<WorkoutPlanPages.AssignModel>("StaffOrAdmin");
    }

    [Fact]
    public void Member_invoice_page_allows_member_policy()
    {
        AssertPolicy<PaymentPages.InvoiceModel>("StaffOrAdminOrMember");
    }

    private static void AssertPolicy<TPage>(string expectedPolicy)
        where TPage : PageModel
    {
        var attribute = typeof(TPage)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal(expectedPolicy, attribute!.Policy);
    }
}
