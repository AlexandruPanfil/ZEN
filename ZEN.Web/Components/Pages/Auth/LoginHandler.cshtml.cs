using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ZEN.Web.Components.Pages.Auth;

public class LoginHandlerModel : PageModel
{
    public async Task<IActionResult> OnGetAsync(
        string userId, string userName, string email,
        string roles, string returnUrl = "/")
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            return RedirectToPage("/login");

        var roleList = roles?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Email, email)
        };

        claims.AddRange(roleList.Select(r => new Claim(ClaimTypes.Role, r.Trim())));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // Validate the return URL to prevent open redirect attacks
        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            returnUrl = "/";

        return Redirect(returnUrl);
    }
}