using Microsoft.EntityFrameworkCore;

namespace ChastilianMotorFactors.Infrastructure.Persistence
{
    public sealed class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Picks up every IEntityTypeConfiguration<T> in this assembly
            // (one configuration class per entity, under Persistence/Configurations/).
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}