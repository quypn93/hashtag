using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HashTag.Filters;

public class AdminAuthFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var isAdmin = context.HttpContext.Session.GetString("IsAdmin");

        if (isAdmin != "true")
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed after execution
    }
}
