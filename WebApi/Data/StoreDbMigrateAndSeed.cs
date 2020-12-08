using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi.Data
{
    public static class StoreDbMigrateAndSeed
    {
        public static void MigrateAndSeedData(this IApplicationBuilder app, bool development = false)
        {
            using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();

            using var context = serviceScope.ServiceProvider.GetService<StoreDbContext>();

            //your development/live logic here eg:
            context.Migrate();

            if (development)
                context.Seed();
        }

        private static void Migrate(this StoreDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Database.GetPendingMigrations().Any())
                context.Database.Migrate();
        }

        private static void Seed(this StoreDbContext context)
        {
            context.SeedData();

            context.SaveChanges();
        }
    }
}
