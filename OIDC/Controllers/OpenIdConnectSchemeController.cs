using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace OIDC.Controllers;

public class OpenIdConnectSchemeController : Controller
{
    private readonly OidcDbContext _context;
    private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;
    private readonly IOptionsMonitorCache<OpenIdConnectOptions> _optionsMonitorCache;
    private readonly OpenIdConnectPostConfigureOptions _openIdConnectPostConfigureOptions;

    public OpenIdConnectSchemeController(
        OidcDbContext context, 
        IAuthenticationSchemeProvider authenticationSchemeProvider, 
        IOptionsMonitorCache<OpenIdConnectOptions> optionsMonitorCache, 
        OpenIdConnectPostConfigureOptions openIdConnectPostConfigureOptions
        )
    {
        _context = context;
        _authenticationSchemeProvider = authenticationSchemeProvider;
        _optionsMonitorCache = optionsMonitorCache;
        _openIdConnectPostConfigureOptions = openIdConnectPostConfigureOptions;
    }

    // GET: OpenIdConnectScheme
    public async Task<IActionResult> Index()
    {
        return View(await _context.OpenIdConnectSchemes.ToListAsync());
    }

    // GET: OpenIdConnectScheme/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: OpenIdConnectScheme/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Authority,ClientId,ClientSecret")] OpenIdConnectScheme openIdConnectScheme)
    {
        if (ModelState.IsValid)
        {
            _context.Add(openIdConnectScheme);
            await _context.SaveChangesAsync();
            await LoadScheme(openIdConnectScheme);
            return RedirectToAction(nameof(Index));
        }
        return View(openIdConnectScheme);
    }

    // GET: OpenIdConnectScheme/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var openIdConnectScheme = await _context.OpenIdConnectSchemes.FindAsync(id);
        if (openIdConnectScheme == null)
        {
            return NotFound();
        }
        return View(openIdConnectScheme);
    }

    // POST: OpenIdConnectScheme/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Authority,ClientId,ClientSecret")] OpenIdConnectScheme openIdConnectScheme)
    {
        if (id != openIdConnectScheme.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(openIdConnectScheme);
                await _context.SaveChangesAsync();
                await LoadScheme(openIdConnectScheme);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OpenIdConnectSchemeExists(openIdConnectScheme.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(openIdConnectScheme);
    }

    // GET: OpenIdConnectScheme/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var openIdConnectScheme = await _context.OpenIdConnectSchemes
            .FirstOrDefaultAsync(m => m.Id == id);
        if (openIdConnectScheme == null)
        {
            return NotFound();
        }

        return View(openIdConnectScheme);
    }

    // GET: OpenIdConnectScheme/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var openIdConnectScheme = await _context.OpenIdConnectSchemes
            .FirstOrDefaultAsync(m => m.Id == id);
        if (openIdConnectScheme == null)
        {
            return NotFound();
        }

        return View(openIdConnectScheme);
    }

    // POST: OpenIdConnectScheme/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var openIdConnectScheme = await _context.OpenIdConnectSchemes.FindAsync(id);
        _context.OpenIdConnectSchemes.Remove(openIdConnectScheme);
        await _context.SaveChangesAsync();

        var key = $"oidc-{openIdConnectScheme.Id}";
        _authenticationSchemeProvider.RemoveScheme(key);
        _optionsMonitorCache.TryRemove(key);
        
        return RedirectToAction(nameof(Index));
    }

    private bool OpenIdConnectSchemeExists(int id)
    {
        return _context.OpenIdConnectSchemes.Any(e => e.Id == id);
    }
    
    private async Task LoadScheme(OpenIdConnectScheme scheme)
    {
        // формируем уникальный ключ для схемы аутентификации
        var key = "oidc-" + scheme.Id;
        var options = new OpenIdConnectOptions();
        options.CallbackPath = $"/signin-{key}";
        options.Authority = scheme.Authority;
        options.ClientId = scheme.ClientId;
        options.ClientSecret = scheme.ClientSecret;
        options.UsePkce = true;
        options.RequireHttpsMetadata = false;
        options.SaveTokens = true;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.ResponseMode = OpenIdConnectResponseMode.FormPost;
        options.NonceCookie.SameSite = SameSiteMode.Unspecified;
        options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;
        
        var existingScheme = await _authenticationSchemeProvider.GetSchemeAsync(key);
        // если такой схемы ещё нет, значит просто добавляем её
        if (existingScheme is null)
        {
            AddScheme(key, options);
        }
        else
        {
            // если схема уже есть, то заменяем её в _authenticationSchemeProvider и обновляем кэши
            _authenticationSchemeProvider.RemoveScheme(key);
            _optionsMonitorCache.TryRemove(key);
            AddScheme(key, options);
        }
    }

    private void AddScheme(string key, OpenIdConnectOptions options)
    {
        _authenticationSchemeProvider.AddScheme(new AuthenticationScheme(key, key, typeof(OpenIdConnectHandler)));
        _openIdConnectPostConfigureOptions.PostConfigure(key, options);
        _optionsMonitorCache.TryAdd(key, options);
    }
}