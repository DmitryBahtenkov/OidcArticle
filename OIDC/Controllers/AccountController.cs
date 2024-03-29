using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OIDC.Controllers;

public class AccountController : Controller
{
    private readonly OidcDbContext _dbContext;

    public AccountController(OidcDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _dbContext.OpenIdConnectSchemes.ToListAsync());
    }

    [HttpPost]
    public IActionResult Login(string scheme)
    {
        if (HttpContext.User.Identity?.IsAuthenticated is not true)
        {
            return Challenge(new OpenIdConnectChallengeProperties
            {
                RedirectUri = $"/account/callback?scheme={scheme}",
            }, scheme);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet("account/callback")]
    public async Task<IActionResult> OpenIdCallback(string scheme)
    {
        await HttpContext.AuthenticateAsync(scheme);
        return RedirectToAction("Index", "Home");
    }
}