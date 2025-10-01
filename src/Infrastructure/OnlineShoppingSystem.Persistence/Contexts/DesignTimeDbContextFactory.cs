using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OnlineShoppingSystem.Persistence.Contexts;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OnlineShoppingSystemDbContext>
{
    public OnlineShoppingSystemDbContext CreateDbContext(string[] args)
    {
        
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) 
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<OnlineShoppingSystemDbContext>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseSqlServer(connectionString);

        return new OnlineShoppingSystemDbContext(builder.Options);
    }
}
