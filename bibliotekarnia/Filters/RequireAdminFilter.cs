using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace bibliotekarnia.Filters;

public class RequireAdminFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var userId = context.HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        var isAdmin = context.HttpContext.Session.GetString("IsAdmin");
        if (isAdmin != "true")
        {
            context.Result = new StatusCodeResult(403);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
