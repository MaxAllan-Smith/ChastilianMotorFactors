using ChastilianMotorFactors.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChastilianMotorFactors.Infrastructure.Persistence
{
    // Holds the ASP.NET Core Identity tables (users, roles, ...) and, via UseOpenIddict()
    // in DependencyInjection, the OpenIddict tables (applications, authorizations, scopes, tokens).
    public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configures the Identity tables; must run before the project's own configurations.
            base.OnModelCreating(modelBuilder);

            // Picks up every IEntityTypeConfiguration<T> in this assembly
            // (one configuration class per entity, under Persistence/Configurations/).
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}