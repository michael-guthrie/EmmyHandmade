using System.Diagnostics;
using Npgsql;

namespace AssetManager.Blazor.Services;

/// <summary>
/// The PostgreSQL stand-in for the WPF File &gt; Export data command, which ran
/// <c>BACKUP DATABASE @name TO DISK=@path WITH FORMAT</c> and wrote a <c>.bak</c> the user chose
/// with a save dialog.
///
/// PostgreSQL has no in-SQL equivalent — backups are taken by the <c>pg_dump</c> client tool — so
/// this shells out to it and streams the result to the browser as a download. The dump is written
/// in PostgreSQL's custom format, which is the closest analogue to a SQL Server <c>.bak</c>:
/// compressed, and restorable whole with <c>pg_restore</c>.
///
/// <para><b>pg_dump must be installed and its major version must be at least the server's.</b>
/// <see cref="GetDiagnosticsAsync"/> reports what is actually available so the page can say
/// what is wrong rather than failing silently.</para>
/// </summary>
public sealed class DatabaseBackupService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(IConfiguration configuration, ILogger<DatabaseBackupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Path to pg_dump. Override with <c>Backup:PgDumpPath</c> when it is not on PATH — on Windows
    /// that is typically <c>C:\Program Files\PostgreSQL\17\bin\pg_dump.exe</c>.
    /// </summary>
    private string PgDumpPath => _configuration["Backup:PgDumpPath"] ?? "pg_dump";

    private string ConnectionString =>
        _configuration.GetConnectionString("TrackingStore")
        ?? throw new InvalidOperationException("Connection string 'TrackingStore' was not found.");

    public string SuggestedFileName =>
        $"AssetManagerData_{DateTime.Now:yyyy-MM-dd_HHmm}.backup";

    /// <summary>Checks whether a backup can actually be taken, and why not if it cannot.</summary>
    public async Task<BackupDiagnostics> GetDiagnosticsAsync(CancellationToken ct = default)
    {
        string? version = null;
        string? error = null;

        try
        {
            var psi = BaseStartInfo();
            psi.ArgumentList.Add("--version");

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Process.Start returned null.");

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0)
            {
                version = stdout.Trim();
            }
            else
            {
                error = string.IsNullOrWhiteSpace(stderr) ? $"pg_dump exited with {process.ExitCode}." : stderr.Trim();
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        var builder = new NpgsqlConnectionStringBuilder(ConnectionString);

        return new BackupDiagnostics(
            IsAvailable: version is not null,
            PgDumpPath: PgDumpPath,
            PgDumpVersion: version,
            Error: error,
            DatabaseName: builder.Database ?? "(not set)",
            Host: builder.Host ?? "(not set)");
    }

    /// <summary>
    /// Runs pg_dump and writes the dump into <paramref name="destination"/>.
    /// Throws <see cref="BackupFailedException"/> with pg_dump's own message when it fails.
    /// </summary>
    public async Task WriteBackupAsync(Stream destination, CancellationToken ct = default)
    {
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString);

        var psi = BaseStartInfo();

        // No --file: pg_dump writes the dump to stdout, which is read below. Naming a file such as
        // /dev/stdout would be both platform-specific and wrong, since pg_dump treats a named file
        // as seekable.
        psi.ArgumentList.Add("--format=custom");
        psi.ArgumentList.Add("--no-owner");
        psi.ArgumentList.Add("--no-privileges");

        if (!string.IsNullOrEmpty(builder.Host)) psi.ArgumentList.Add($"--host={builder.Host}");
        if (builder.Port != 0) psi.ArgumentList.Add($"--port={builder.Port}");
        if (!string.IsNullOrEmpty(builder.Username)) psi.ArgumentList.Add($"--username={builder.Username}");
        if (!string.IsNullOrEmpty(builder.Database)) psi.ArgumentList.Add($"--dbname={builder.Database}");

        // Never passed on the command line, where it would show in the process list.
        psi.Environment["PGPASSWORD"] = builder.Password ?? string.Empty;

        using var process = Process.Start(psi)
            ?? throw new BackupFailedException("Could not start pg_dump.");

        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.StandardOutput.BaseStream.CopyToAsync(destination, ct);
        var stderr = await stderrTask;
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            _logger.LogError("pg_dump failed with exit code {Code}: {Error}", process.ExitCode, stderr);
            throw new BackupFailedException(
                string.IsNullOrWhiteSpace(stderr)
                    ? $"pg_dump exited with code {process.ExitCode}."
                    : stderr.Trim());
        }
    }

    private ProcessStartInfo BaseStartInfo() => new()
    {
        FileName = PgDumpPath,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };
}

public record BackupDiagnostics(
    bool IsAvailable,
    string PgDumpPath,
    string? PgDumpVersion,
    string? Error,
    string DatabaseName,
    string Host);

public class BackupFailedException : Exception
{
    public BackupFailedException(string message) : base(message) { }
}
