using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Security;

namespace Valuator.Pages;

public sealed class RegisterModel : PageModel
{
    private readonly UserStore _users;

    public RegisterModel(UserStore users) => _users = users;

    [BindProperty]
    [Required, Display(Name = "Логин")]
    public string Login { get; set; } = string.Empty;

    [BindProperty]
    [Required, MinLength(8), DataType(DataType.Password), Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true
        ? RedirectToPage("/Index")
        : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!UserStore.IsValidLogin(Login))
        {
            ModelState.AddModelError(nameof(Login),
                "Логин: 3–50 символов, латинские буквы, цифры, _, - или точка.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await _users.RegisterAsync(Login.Trim(), Password))
        {
            ModelState.AddModelError(nameof(Login), "Такой логин уже зарегистрирован.");
            return Page();
        }

        return RedirectToPage("/Login", new { registered = true });
    }
}
