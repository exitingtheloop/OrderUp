using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderUp.API.Data;
using OrderUp.API.Data.Seed;

namespace OrderUp.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory that replaces SQL Server with SQLite in-memory for testing.
/// Each test gets an isolated database via unique connection string.
/// </summary>
public class OrderUpWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;
    private readonly string? _testUserRole;

    public OrderUpWebApplicationFactory(string? testUserRole = null)
    {
        _testUserRole = testUserRole;
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

        // Configure test authentication if a role is specified
        if (_testUserRole != null)
        {
            builder.ConfigureTestServices(services =>
            {
                // Store the role for the test handler
                services.AddSingleton(new TestAuthConfig { Role = _testUserRole });

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        }
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

/// <summary>
/// Configuration for test authentication.
/// </summary>
public class TestAuthConfig
{
    public string? Role { get; set; }
}

/// <summary>
/// Test authentication handler that creates a user with the configured role.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly TestAuthConfig _config;

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TestAuthConfig config)
        : base(options, logger, encoder)
    {
        _config = config;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testuser@test.local"),
            new(ClaimTypes.NameIdentifier, "test-user-id"),
            new(ClaimTypes.Email, "testuser@test.local")
        };

        if (!string.IsNullOrEmpty(_config.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, _config.Role));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
