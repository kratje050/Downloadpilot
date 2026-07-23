using Microsoft.Data.Sqlite;

namespace DownloadPilot.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory
{
    private readonly string _databasePath;

    public SqliteConnectionFactory(string? databasePath = null)
    {
        _databasePath = string.IsNullOrWhiteSpace(databasePath)
            ? SqlitePaths.DatabasePath
            : databasePath;
    }

    public string DatabasePath => _databasePath;

    public SqliteConnection CreateOpenConnection()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }
}
