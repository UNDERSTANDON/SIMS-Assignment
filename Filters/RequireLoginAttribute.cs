using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SIMS_WEB.Filters
{
    /// <summary>Redirects unauthenticated users to Login.</summary>
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public string[] AllowedRoles { get; set; } = Array.Empty<string>();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(role))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (AllowedRoles.Length > 0 && !AllowedRoles.Contains(role))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}
