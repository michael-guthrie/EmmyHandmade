using AssetManager.Blazor.Components;
using AssetManager.Blazor.Services;
using AssetManager.Core.Data;
using AssetManager.Core.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

var connectionString = builder.Configuration.GetConnectionString("TrackingStore")
    ?? throw new InvalidOperationException(
        "Connection string 'TrackingStore' was not found. Set it in appsettings.json or via the " +
        "ConnectionStrings__TrackingStore environment variable.");

// A factory rather than a scoped DbContext: in Blazor Server a "scope" is the whole circuit, which
// lives as long as the browser tab. A shared context would accumulate tracked entities for hours
// and is not safe across concurrent renders.
builder.Services.AddDbContextFactory<TrackingStore>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<InventoryCalculationService>();
builder.Services.AddScoped<BatchCostingService>();
builder.Services.AddScoped<ExpenseReportService>();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<DatabaseBackupService>();

var app = builder.Build();

// Reference data is seeded at startup rather than through HasData, so that it never conflicts
// with a database the desktop app already populated. The call is idempotent and additive.
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TrackingStore>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await SeedData.EnsureSeededAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

// Database backup download — the stand-in for the WPF File > Export data command. A plain endpoint
// rather than JS interop, so the browser's own save dialog does the work the WPF SaveFileDialog did.
//
// NOTE: like the rest of this app, this endpoint is unauthenticated. It hands out the entire
// database, so do not expose this app to an untrusted network without putting authentication in
// front of it. See README-PORT.md.
app.MapGet("/export/database", async (
    DatabaseBackupService backup,
    HttpContext http,
    CancellationToken ct) =>
{
    var diagnostics = await backup.GetDiagnosticsAsync(ct);
    if (!diagnostics.IsAvailable)
    {
        return Results.Problem(
            title: "Backup unavailable",
            detail: diagnostics.Error ?? "pg_dump could not be run.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    http.Response.ContentType = "application/octet-stream";
    http.Response.Headers.ContentDisposition =
        $"attachment; filename=\"{backup.SuggestedFileName}\"";

    try
    {
        await backup.WriteBackupAsync(http.Response.Body, ct);
    }
    catch (BackupFailedException)
    {
        // Headers are already sent by this point, so the response cannot be turned into an error
        // page. Aborting marks the download as failed rather than handing over a truncated file
        // that looks like a valid backup.
        http.Abort();
    }

    return Results.Empty;
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
