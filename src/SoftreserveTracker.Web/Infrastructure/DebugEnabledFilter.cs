using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SoftreserveTracker.Web.Infrastructure;

public sealed class DebugEnabledFilter(IWebHostEnvironment env) : IActionFilter
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
        env.IsDevelopment();
}

public sealed class DebugEnabledAttribute : ServiceFilterAttribute
{
    public DebugEnabledAttribute() : base(typeof(DebugEnabledFilter))
    {
    }
}
