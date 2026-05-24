using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SoftreserveTracker.Web.Infrastructure;

public sealed class DebugEnabledFilter(IWebHostEnvironment env, IConfiguration configuration) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!IsDebugEnabled())
        {
            context.Result = new NotFoundResult();
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private bool IsDebugEnabled() =>
        env.IsDevelopment() || configuration.GetValue("Debug:Enabled", false);
}

public sealed class DebugEnabledAttribute : ServiceFilterAttribute
{
    public DebugEnabledAttribute() : base(typeof(DebugEnabledFilter))
    {
    }
}
