using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Web.Pages.Admin;
using Rackroom.Web.Pages.Admin.Churn;
using Rackroom.Web.Pages.Attendance;
using Rackroom.Web.Pages.MemberPortal;
using Rackroom.Web.Pages.Members;
using Rackroom.Web.Pages.Memberships;
using Rackroom.Web.Pages.Payments;
using Rackroom.Web.Pages.Plans;
using Rackroom.Web.Pages.WorkoutPlans;

namespace Rackroom.Tests;

public class Stage8AuthorizationReviewTests
{
    [Fact]
    public void Admin_pages_require_admin_policy()
    {
        AssertPolicy<DashboardModel>("AdminOnly");
        AssertPolicy<Churn.IndexModel>("AdminOnly");
        AssertPolicy<Churn.TrainModel>("AdminOnly");
    }

    [Fact]
    public void Member_portal_pages_require_member_policy()
    {
        AssertPolicy<MemberPortal.IndexModel>("MemberOnly");
        AssertPolicy<MemberPortal.PauseModel>("MemberOnly");
        AssertPolicy<MemberPortal.CancelModel>("MemberOnly");
        AssertPolicy<MemberPortal.MeasurementModel>("MemberOnly");
    }

    [Fact]
    public void Staff_pages_require_staff_or_admin_policy()
    {
        AssertPolicy<Members.IndexModel>("StaffOrAdmin");
        AssertPolicy<Members.CreateModel>("StaffOrAdmin");
        AssertPolicy<Members.EditModel>("StaffOrAdmin");
        AssertPolicy<Members.DeactivateModel>("StaffOrAdmin");
        AssertPolicy<Memberships.IndexModel>("StaffOrAdmin");
        AssertPolicy<Memberships.CreateModel>("StaffOrAdmin");
        AssertPolicy<Memberships.PauseModel>("StaffOrAdmin");
        AssertPolicy<Memberships.ApprovePauseModel>("StaffOrAdmin");
        AssertPolicy<Memberships.CancelModel>("StaffOrAdmin");
        AssertPolicy<Plans.IndexModel>("StaffOrAdmin");
        AssertPolicy<Plans.CreateModel>("StaffOrAdmin");
        AssertPolicy<Payments.IndexModel>("StaffOrAdmin");
        AssertPolicy<Payments.CreateModel>("StaffOrAdmin");
        AssertPolicy<Attendance.DeskModel>("StaffOrAdmin");
        AssertPolicy<Attendance.StaffCheckInModel>("StaffOrAdmin");
        AssertPolicy<WorkoutPlans.IndexModel>("StaffOrAdmin");
        AssertPolicy<WorkoutPlans.CreateModel>("StaffOrAdmin");
        AssertPolicy<WorkoutPlans.AssignModel>("StaffOrAdmin");
    }

    [Fact]
    public void Member_invoice_page_allows_member_policy()
    {
        AssertPolicy<InvoiceModel>("StaffOrAdminOrMember");
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
