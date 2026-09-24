using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Security;

namespace Valuator.Pages;

public sealed class LoginModel : PageModel
{
    private readonly UserStore _users;

    public LoginModel(UserStore users) => _users = users;

    [BindProperty]
    [Required, Display(Name = "Логин")]
    public string Login { get; set; } = string.Empty;

    [BindProperty]
    [Required, DataType(DataType.Password), Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public bool Registered { get; private set; }

    public IActionResult OnGet(bool registered = false, string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        Registered = registered;
        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || !await _users.ValidateAsync(Login, Password))
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
            return Page();
        }

        string canonicalLogin = await _users.GetCanonicalLoginAsync(Login) ?? Login;
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, canonicalLogin),
                new Claim(ClaimTypes.Name, canonicalLogin)
            },
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : RedirectToPage("/Index");
    }
}
