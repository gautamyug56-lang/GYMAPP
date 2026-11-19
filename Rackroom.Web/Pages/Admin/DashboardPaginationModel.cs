namespace Rackroom.Web.Pages.Admin;

public sealed record DashboardPaginationModel(
    int CurrentPage,
    int PageCount,
    string QueryParameter);
