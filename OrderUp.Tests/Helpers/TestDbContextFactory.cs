using Microsoft.EntityFrameworkCore;
using OrderUp.API.Data;

namespace OrderUp.Tests.Helpers;

public static class TestDbContextFactory
{
    public static DataContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString()).Options;

        return new DataContext(options);
    }
}
