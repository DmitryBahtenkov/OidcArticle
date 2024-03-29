using Microsoft.EntityFrameworkCore;

namespace OIDC;

public class OidcDbContext : DbContext
{
    public OidcDbContext(DbContextOptions<OidcDbContext> options)
        : base(options)
    {
        Database.EnsureCreated();
    }


    public DbSet<OpenIdConnectScheme> OpenIdConnectSchemes { get; set; }
}
