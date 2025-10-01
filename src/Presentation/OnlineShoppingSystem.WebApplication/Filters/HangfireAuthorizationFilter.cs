using Hangfire.Dashboard;

namespace OnlineShoppingSystem.WebApplication.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        
        return httpContext.User.Identity != null
               && httpContext.User.Identity.IsAuthenticated
               && httpContext.User.IsInRole("Admin");
    }
}

