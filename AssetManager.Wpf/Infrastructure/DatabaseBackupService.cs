using AssetManager.Core.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Infrastructure;

/// <summary>
/// The File &rsaquo; Export data command, carried over unchanged in substance from the .NET
/// Framework app: <c>BACKUP DATABASE @name TO DISK = @path WITH FORMAT</c>.
///
/// <para>Both the database name and the path are passed as parameters, which T-SQL allows for
/// BACKUP, so a path containing a quote cannot break out into the statement.</para>
///
/// <para>One thing worth knowing, which was equally true of the old app: the path is resolved by
/// <em>SQL Server</em>, not by this process. With LocalDB they are the same machine, so it behaves
/// like an ordinary save. Against a remote server the file lands on the server, and the SQL Server
/// service account needs permission to write there.</para>
/// </summary>
public sealed class DatabaseBackupService
{
    private readonly IDbContextFactory<TrackingStore> _dbFactory;

    public DatabaseBackupService(IDbContextFactory<TrackingStore> dbFactory) => _dbFactory = dbFactory;

    public string SuggestedFileName => $"AssetManagerData_{DateTime.Now:yyyy-MM-dd_HHmm}.bak";

    /// <summary>
    /// Backs the database up to <paramref name="destinationPath"/>.
    /// Throws <see cref="BackupFailedException"/> with SQL Server's own message on failure.
    /// </summary>
    public async Task BackupAsync(string destinationPath, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var databaseName = await ResolveDatabaseNameAsync(db, ct);
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new BackupFailedException(
                "Could not work out which database to back up. Set Initial Catalog in the connection string.");
        }

        try
        {
            // WITH FORMAT overwrites rather than appending to an existing backup set, which is
            // what the old app did and what the overwrite prompt in the save dialog implies.
            await db.Database.ExecuteSqlRawAsync(
                "BACKUP DATABASE @p0 TO DISK = @p1 WITH FORMAT;",
                [databaseName, destinationPath],
                ct);
        }
        catch (SqlException ex)
        {
            throw new BackupFailedException(BuildFailureMessage(ex, databaseName, destinationPath), ex);
        }
    }

    private static async Task<string?> ResolveDatabaseNameAsync(TrackingStore db, CancellationToken ct)
    {
        var fromConnection = db.Database.GetDbConnection().Database;
        if (!string.IsNullOrWhiteSpace(fromConnection)) return fromConnection;

        // An attached .mdf with no Initial Catalog gets a generated name; ask the server for it.
        // The old app scraped sys.databases with a LIKE on the file path to do the same thing.
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT DB_NAME();";
            var result = await command.ExecuteScalarAsync(ct);
            return result as string;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static string BuildFailureMessage(SqlException ex, string databaseName, string path) =>
        ex.Number switch
        {
            // Operating system error on the path SQL Server tried to write.
            3201 => $"SQL Server could not write to '{path}'. The path is resolved by SQL Server, "
                  + $"not by this program, and its service account needs write access to that folder."
                  + Environment.NewLine + Environment.NewLine + ex.Message,

            // BACKUP is not permitted for the login.
            262 or 300 => $"The SQL Server login does not have permission to back up '{databaseName}'."
                        + Environment.NewLine + Environment.NewLine + ex.Message,

            _ => ex.Message,
        };
}

public class BackupFailedException : Exception
{
    public BackupFailedException(string message, Exception? inner = null) : base(message, inner) { }
}
