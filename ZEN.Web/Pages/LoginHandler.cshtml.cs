using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace ZEN.Web.Pages;

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

        var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);

        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            returnUrl = "/";

        return Redirect(returnUrl);
    }
}