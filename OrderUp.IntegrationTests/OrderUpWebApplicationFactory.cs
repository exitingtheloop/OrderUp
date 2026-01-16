using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderUp.API.Data;

namespace OrderUp.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory that replaces SQL Server with SQLite in-memory for testing.
/// Each test gets an isolated database via unique connection string.
/// </summary>
public class OrderUpWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public OrderUpWebApplicationFactory()
    {
        // Create and open the connection - must stay open for in-memory SQLite
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<DataContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add SQLite in-memory database for testing
                services.AddDbContext<DataContext>(options =>
                   {
                       options.UseSqlite(_connection);
                   });

                // Build the service provider and ensure the database is created
                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                context.Database.EnsureCreated();
            });
    }

    /// <summary>
    /// Gets a scoped DataContext for seeding test data.
    /// </summary>
    public DataContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<DataContext>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
