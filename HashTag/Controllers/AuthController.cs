using Microsoft.AspNetCore.Mvc;

namespace HashTag.Controllers;

public class AuthController : Controller
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public IActionResult Login(string password, string? returnUrl = null)
    {
        var adminPassword = _configuration.GetValue<string>("AdminSettings:Password") ?? "admin123";

        if (password == adminPassword)
        {
            HttpContext.Session.SetString("IsAdmin", "true");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Admin");
        }

        ModelState.AddModelError("", "Invalid password");
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
