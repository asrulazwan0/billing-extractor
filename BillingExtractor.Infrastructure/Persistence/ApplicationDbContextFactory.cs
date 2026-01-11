using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace BillingExtractor.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                              "Data Source=billing_extractor_dev.db";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use same logic as DependencyInjection to determine provider
        if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseSqlite(connectionString, b => b.MigrationsAssembly("BillingExtractor.Infrastructure"));
        }
        else
        {
            optionsBuilder.UseSqlServer(connectionString, b => b.MigrationsAssembly("BillingExtractor.Infrastructure"));
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}