using Notification.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Notification.Application.Tests.Support;

internal sealed class NotificationTestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private bool _initialized;

    public NotificationDbContext CreateContext()
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }

        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new NotificationDbContext(options);

        if (!_initialized)
        {
            context.Database.EnsureCreated();
            _initialized = true;
        }

        return context;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
