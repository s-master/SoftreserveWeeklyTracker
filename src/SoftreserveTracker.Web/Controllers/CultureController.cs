using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace SoftreserveTracker.Web.Controllers;

public class CultureController : Controller
{
    [HttpGet("/culture/set")]
    public IActionResult Set(string culture, string returnUrl)
    {
        if (culture is not ("de" or "en"))
        {
            culture = "de";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            return RedirectToAction("Index", "Home");
        }

        return LocalRedirect(returnUrl);
    }
}
