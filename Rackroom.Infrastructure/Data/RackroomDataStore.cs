using Microsoft.EntityFrameworkCore;
using Rackroom.Application.Abstractions;
using Rackroom.Domain.Entities;

namespace Rackroom.Infrastructure.Data;

public sealed class RackroomDataStore : IRackroomDataStore
{
    private readonly ApplicationDbContext _db;

    public RackroomDataStore(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Member?> GetMemberAsync(
        Guid gymId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        return _db.Members
            .SingleOrDefaultAsync(
                x => x.GymId == gymId && x.Id == memberId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Member>> SearchMembersAsync(
        Guid gymId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Member> query = _db.Members
            .AsNoTracking()
            .Where(x => x.GymId == gymId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();

            query = query.Where(x =>
                x.FirstName.ToLower().Contains(term) ||
                x.LastName.ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term));
        }

        return await query
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> MemberEmailExistsAsync(
        Guid gymId,
        string email,
        Guid? excludedMemberId = null,
        CancellationToken cancellationToken = default)
    {
        return _db.Members.AnyAsync(
            x => x.GymId == gymId &&
                 x.Email == email &&
                 (!excludedMemberId.HasValue || x.Id != excludedMemberId.Value),
            cancellationToken);
    }

    public Task AddMemberAsync(
        Member member,
        CancellationToken cancellationToken = default)
    {
        return _db.Members.AddAsync(member, cancellationToken).AsTask();
    }

    public Task<MembershipPlan?> GetMembershipPlanAsync(
        Guid gymId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPlans
            .SingleOrDefaultAsync(
                x => x.GymId == gymId && x.Id == planId,
                cancellationToken);
    }

    public Task<bool> MembershipPlanNameExistsAsync(
        Guid gymId,
        string name,
        Guid? excludedPlanId = null,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPlans.AnyAsync(
            x => x.GymId == gymId &&
                 x.Name == name &&
                 (!excludedPlanId.HasValue || x.Id != excludedPlanId.Value),
            cancellationToken);
    }

    public async Task<IReadOnlyList<MembershipPlan>> GetMembershipPlansAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _db.MembershipPlans
            .AsNoTracking()
            .Where(x => x.GymId == gymId)
            .OrderBy(x => x.DurationMonths)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task AddMembershipPlanAsync(
        MembershipPlan plan,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPlans.AddAsync(plan, cancellationToken).AsTask();
    }

    public Task<Membership?> GetMembershipAsync(
        Guid gymId,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        return _db.Memberships
            .Include(x => x.Pauses)
            .Include(x => x.CancellationRecord)
            .SingleOrDefaultAsync(
                x => x.GymId == gymId && x.Id == membershipId,
                cancellationToken);
    }

    public Task<bool> HasActiveMembershipAsync(
        Guid gymId,
        Guid memberId,
        DateTime atUtc,
        CancellationToken cancellationToken = default)
    {
        return _db.Memberships.AnyAsync(
            x => x.GymId == gymId &&
                 x.MemberId == memberId &&
                 x.Status == MembershipStatus.Active &&
                 x.StartDateUtc <= atUtc &&
                 x.EndDateUtc > atUtc,
            cancellationToken);
    }

    public Task AddMembershipAsync(
        Membership membership,
        CancellationToken cancellationToken = default)
    {
        return _db.Memberships.AddAsync(membership, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<Membership>> GetMembershipsAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Memberships
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.MembershipPlan)
            .Include(x => x.Pauses)
            .Include(x => x.CancellationRecord)
            .Where(x => x.GymId == gymId)
            .OrderByDescending(x => x.StartDateUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<MembershipPause?> GetMembershipPauseAsync(
        Guid gymId,
        Guid pauseId,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPauses
            .SingleOrDefaultAsync(
                x => x.GymId == gymId && x.Id == pauseId,
                cancellationToken);
    }

    public Task<int> CountApprovedPausesInCalendarYearAsync(
        Guid gymId,
        Guid membershipId,
        int year,
        CancellationToken cancellationToken = default)
    {
        DateTime yearStart = new(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime nextYearStart = yearStart.AddYears(1);

        return _db.MembershipPauses.CountAsync(
            x => x.GymId == gymId &&
                 x.MembershipId == membershipId &&
                 x.Status == PauseStatus.Approved &&
                 x.StartDateUtc >= yearStart &&
                 x.StartDateUtc < nextYearStart,
            cancellationToken);
    }

    public Task<bool> HasOverlappingPauseAsync(
        Guid gymId,
        Guid membershipId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPauses.AnyAsync(
            x => x.GymId == gymId &&
                 x.MembershipId == membershipId &&
                 x.Status != PauseStatus.Rejected &&
                 x.StartDateUtc <= endDateUtc &&
                 x.EndDateUtc >= startDateUtc,
            cancellationToken);
    }

    public Task AddMembershipPauseAsync(
        MembershipPause pause,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPauses.AddAsync(pause, cancellationToken).AsTask();
    }

    public Task<bool> CancellationExistsAsync(
        Guid gymId,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        return _db.CancellationRecords.AnyAsync(
            x => x.GymId == gymId && x.MembershipId == membershipId,
            cancellationToken);
    }

    public Task AddCancellationRecordAsync(
        CancellationRecord cancellationRecord,
        CancellationToken cancellationToken = default)
    {
        return _db.CancellationRecords.AddAsync(cancellationRecord, cancellationToken).AsTask();
    }



    public Task<Membership?> GetMembershipForPaymentAsync(
        Guid gymId,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        return _db.Memberships
            .Include(x => x.Member)
                .ThenInclude(x => x!.Gym)
            .Include(x => x.MembershipPlan)
            .SingleOrDefaultAsync(
                x => x.GymId == gymId && x.Id == membershipId,
                cancellationToken);
    }

    public async Task AddPaymentAndInvoiceAsync(
        Payment payment,
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);

        int nextInvoiceNumber = (await _db.Invoices
            .Where(x => x.GymId == payment.GymId)
            .Select(x => (int?)x.InvoiceNumber)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        invoice.InvoiceNumber = nextInvoiceNumber;

        _db.Payments.Add(payment);
        _db.Invoices.Add(invoice);

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Payments
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.Membership)
                .ThenInclude(x => x!.MembershipPlan)
            .Include(x => x.Invoice)
            .Where(x => x.GymId == gymId)
            .OrderByDescending(x => x.PaidAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<decimal> GetPaymentTotalAsync(
        Guid gymId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return _db.Payments
            .Where(x => x.GymId == gymId &&
                        x.PaidAtUtc >= fromUtc &&
                        x.PaidAtUtc < toUtc)
            .SumAsync(x => x.Amount, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Membership?> GetActiveMembershipForMemberAsync(
        Guid gymId,
        Guid memberId,
        DateTime atUtc,
        CancellationToken cancellationToken = default)
    {
        return _db.Memberships
            .Include(x => x.MembershipPlan)
            .SingleOrDefaultAsync(
                x => x.GymId == gymId &&
                     x.MemberId == memberId &&
                     x.Status == MembershipStatus.Active &&
                     x.StartDateUtc <= atUtc &&
                     x.EndDateUtc > atUtc,
                cancellationToken);
    }

    public Task AddAttendanceAsync(
        Attendance attendance,
        CancellationToken cancellationToken = default)
    {
        return _db.Attendances.AddAsync(attendance, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<Attendance>> GetMemberAttendanceHistoryAsync(
        Guid gymId,
        Guid memberId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _db.Attendances
            .AsNoTracking()
            .Include(x => x.Member)
            .Where(x => x.GymId == gymId && x.MemberId == memberId)
            .OrderByDescending(x => x.CheckedInAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Attendance>> GetGymAttendanceHistoryAsync(
        Guid gymId,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _db.Attendances
            .AsNoTracking()
            .Include(x => x.Member)
            .Where(x => x.GymId == gymId)
            .OrderByDescending(x => x.CheckedInAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<AttendanceStatisticsData> GetMemberAttendanceStatisticsAsync(
        Guid gymId,
        Guid memberId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await BuildAttendanceStatisticsAsync(
            _db.Attendances.Where(x =>
                x.GymId == gymId &&
                x.MemberId == memberId &&
                x.CheckedInAtUtc >= fromUtc &&
                x.CheckedInAtUtc < toUtc),
            cancellationToken);
    }

    public async Task<AttendanceStatisticsData> GetGymAttendanceStatisticsAsync(
        Guid gymId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await BuildAttendanceStatisticsAsync(
            _db.Attendances.Where(x =>
                x.GymId == gymId &&
                x.CheckedInAtUtc >= fromUtc &&
                x.CheckedInAtUtc < toUtc),
            cancellationToken);
    }

    private static async Task<AttendanceStatisticsData> BuildAttendanceStatisticsAsync(
        IQueryable<Attendance> query,
        CancellationToken cancellationToken)
    {
        int totalCheckIns = await query.CountAsync(cancellationToken);

        DateTime nowUtc = DateTime.UtcNow;
        DateTime monthStart = new(
            nowUtc.Year,
            nowUtc.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        DateTime thirtyDaysAgo = nowUtc.AddDays(-30);

        int currentMonthCheckIns = await query.CountAsync(
            x => x.CheckedInAtUtc >= monthStart,
            cancellationToken);

        int last30DaysCheckIns = await query.CountAsync(
            x => x.CheckedInAtUtc >= thirtyDaysAgo,
            cancellationToken);

        int distinctDaysVisited = await query
            .Select(x => x.CheckedInAtUtc.Date)
            .Distinct()
            .CountAsync(cancellationToken);

        return new AttendanceStatisticsData(
            totalCheckIns,
            currentMonthCheckIns,
            last30DaysCheckIns,
            distinctDaysVisited);
    }


    // Stage 5 dashboard queries

    public async Task<DashboardMetricsData> GetDashboardMetricsAsync(
        Guid gymId,
        DateTime monthStartUtc,
        DateTime nextMonthStartUtc,
        DateTime nowUtc,
        DateTime tomorrowUtc,
        DateTime expiryWindowEndUtc,
        DateTime paymentDeadlineEndUtc,
        CancellationToken cancellationToken = default)
    {
        decimal revenue = await _db.Payments
            .Where(x => x.GymId == gymId &&
                        x.PaidAtUtc >= monthStartUtc &&
                        x.PaidAtUtc < nextMonthStartUtc)
            .SumAsync(x => x.Amount, cancellationToken);

        int newMembers = await _db.Members
            .CountAsync(x => x.GymId == gymId &&
                             x.CreatedAtUtc >= monthStartUtc &&
                             x.CreatedAtUtc < nextMonthStartUtc,
                cancellationToken);

        int expiring = await _db.Memberships
            .CountAsync(x => x.GymId == gymId &&
                             x.Status == MembershipStatus.Active &&
                             x.EndDateUtc >= nowUtc &&
                             x.EndDateUtc < expiryWindowEndUtc,
                cancellationToken);

        int paymentDeadlines = await _db.Memberships
            .CountAsync(x => x.GymId == gymId &&
                             x.Status == MembershipStatus.Active &&
                             x.EndDateUtc >= nowUtc &&
                             x.EndDateUtc < paymentDeadlineEndUtc,
                cancellationToken);

        int todaysAttendance = await _db.Attendances
            .CountAsync(x => x.GymId == gymId &&
                             x.CheckedInAtUtc >= monthStartUtc.Date &&
                             x.CheckedInAtUtc < tomorrowUtc,
                cancellationToken);

        return new DashboardMetricsData(
            revenue,
            newMembers,
            expiring,
            paymentDeadlines,
            todaysAttendance);
    }

    public async Task<IReadOnlyList<DashboardChartPoint>> GetRevenueByMonthAsync(
        Guid gymId,
        DateTime firstMonthStartUtc,
        DateTime nextMonthStartUtc,
        CancellationToken cancellationToken = default)
    {
        var payments = await _db.Payments
            .AsNoTracking()
            .Where(x => x.GymId == gymId &&
                        x.PaidAtUtc >= firstMonthStartUtc &&
                        x.PaidAtUtc < nextMonthStartUtc)
            .Select(x => new { x.PaidAtUtc, x.Amount })
            .ToListAsync(cancellationToken);

        return payments
            .GroupBy(x => new { x.PaidAtUtc.Year, x.PaidAtUtc.Month })
            .Select(group => new DashboardChartPoint(
                new DateTime(group.Key.Year, group.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                    .ToString("yyyy-MM"),
                group.Sum(x => x.Amount)))
            .OrderBy(x => x.Label)
            .ToList();
    }

    public async Task<IReadOnlyList<DashboardChartPoint>> GetNewMembersByMonthAsync(
        Guid gymId,
        DateTime firstMonthStartUtc,
        DateTime nextMonthStartUtc,
        CancellationToken cancellationToken = default)
    {
        var members = await _db.Members
            .AsNoTracking()
            .Where(x => x.GymId == gymId &&
                        x.CreatedAtUtc >= firstMonthStartUtc &&
                        x.CreatedAtUtc < nextMonthStartUtc)
            .Select(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return members
            .GroupBy(x => new { x.Year, x.Month })
            .Select(group => new DashboardChartPoint(
                new DateTime(group.Key.Year, group.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                    .ToString("yyyy-MM"),
                group.Count()))
            .OrderBy(x => x.Label)
            .ToList();
    }

    public async Task<DashboardPageData<DashboardMembershipRow>> GetExpiringMembershipsAsync(
        Guid gymId,
        DateTime nowUtc,
        DateTime endUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Membership> query = _db.Memberships
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.MembershipPlan)
            .Where(x => x.GymId == gymId &&
                        x.Status == MembershipStatus.Active &&
                        x.EndDateUtc >= nowUtc &&
                        x.EndDateUtc < endUtc);

        int totalCount = await query.CountAsync(cancellationToken);

        List<DashboardMembershipRow> items = await query
            .OrderBy(x => x.EndDateUtc)
            .ThenBy(x => x.Member!.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DashboardMembershipRow(
                x.Id,
                x.Member!.FirstName + " " + x.Member.LastName,
                x.Member.Email,
                x.MembershipPlan!.Name,
                x.EndDateUtc))
            .ToListAsync(cancellationToken);

        return new DashboardPageData<DashboardMembershipRow>(
            items,
            totalCount,
            page,
            pageSize);
    }

    public async Task<DashboardPageData<DashboardMembershipRow>> GetPaymentDeadlinesAsync(
        Guid gymId,
        DateTime nowUtc,
        DateTime endUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Membership> query = _db.Memberships
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.MembershipPlan)
            .Where(x => x.GymId == gymId &&
                        x.Status == MembershipStatus.Active &&
                        x.EndDateUtc >= nowUtc &&
                        x.EndDateUtc < endUtc);

        int totalCount = await query.CountAsync(cancellationToken);

        List<DashboardMembershipRow> items = await query
            .OrderBy(x => x.EndDateUtc)
            .ThenBy(x => x.Member!.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DashboardMembershipRow(
                x.Id,
                x.Member!.FirstName + " " + x.Member.LastName,
                x.Member.Email,
                x.MembershipPlan!.Name,
                x.EndDateUtc))
            .ToListAsync(cancellationToken);

        return new DashboardPageData<DashboardMembershipRow>(
            items,
            totalCount,
            page,
            pageSize);
    }

    public async Task<DashboardPageData<DashboardAttendanceRow>> GetTodaysAttendanceAsync(
        Guid gymId,
        DateTime todayStartUtc,
        DateTime tomorrowUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Attendance> query = _db.Attendances
            .AsNoTracking()
            .Include(x => x.Member)
            .Where(x => x.GymId == gymId &&
                        x.CheckedInAtUtc >= todayStartUtc &&
                        x.CheckedInAtUtc < tomorrowUtc);

        int totalCount = await query.CountAsync(cancellationToken);

        List<DashboardAttendanceRow> items = await query
            .OrderByDescending(x => x.CheckedInAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DashboardAttendanceRow(
                x.Id,
                x.Member!.FirstName + " " + x.Member.LastName,
                x.CheckedInAtUtc,
                x.Source))
            .ToListAsync(cancellationToken);

        return new DashboardPageData<DashboardAttendanceRow>(
            items,
            totalCount,
            page,
            pageSize);
    }
