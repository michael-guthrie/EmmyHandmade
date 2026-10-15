# AssetManager — .NET 10 port notes

Three projects, two front ends, one copy of the business logic.

```
AssetManager.sln
├── AssetManager.Core/        net10.0          entities, DbContext, all business logic
├── AssetManager.Wpf/         net10.0-windows  WPF desktop app      → SQL Server
└── AssetManager.Blazor/      net10.0          Blazor web app       → PostgreSQL
```

> **The WPF app and the shared library have not been compiled.** They were written in an
> environment without a .NET SDK. The Blazor app's foundation layer built clean before the split;
> everything since has not. See *What to expect on first build*.

---

## The two apps are on different databases

You chose SQL Server for the desktop app and PostgreSQL for the web app, and chose to keep both
long term. That is workable, but it means **two separate copies of the business data that will
diverge the moment both are used.** An order entered on the desktop will not appear on the web,
and inventory levels will drift apart permanently.

The code is arranged so the *logic* cannot diverge — both apps run the identical costing and
valuation from `AssetManager.Core`, and `TrackingStore` is provider-neutral. Pointing both at one
database is a connection-string change in one file and nothing else:

- To put both on SQL Server: change `AssetManager.Blazor`'s `Program.cs` from `UseNpgsql` to
  `UseSqlServer` and give it the desktop app's connection string.
- To put both on PostgreSQL: change `AssetManager.Wpf`'s `App.xaml.cs` from `UseSqlServer` to
  `UseNpgsql`, and swap the SQL Server backup service for the pg_dump one.

Worth doing before the data is far enough apart to be painful to reconcile. Flagging it once
and then leaving it alone.

---

## What changed in the WPF app

| Area | Before | Now |
|---|---|---|
| Framework | .NET Framework 4.7.1 | .NET 10 (`net10.0-windows`) |
| Project | old-style csproj + packages.config | SDK-style csproj |
| ORM | EF6 | EF Core 10 (via Core) |
| MVVM | MvvmLight 5.2 | CommunityToolkit.Mvvm |
| Behaviors | System.Windows.Interactivity | Microsoft.Xaml.Behaviors.Wpf |
| Animated GIF | WpfAnimatedGif 1.4.13 | WpfAnimatedGif 2.x |
| Service location | CommonServiceLocator | Microsoft.Extensions.DependencyInjection |
| Settings | `Properties.Settings` + App.config | `appsettings.json` + a JSON file in %APPDATA% |
| Config | connection string built in code | `appsettings.json` |

### Why MvvmLight had to go

It is not a preference. [MvvmLightLibs on NuGet](https://www.nuget.org/packages/MvvmLightLibs) is
formally marked deprecated, was last published in September 2018, and targets .NET Framework 3.5
at newest. The maintainer's own note reads: *"MVVM Light is not maintained anymore. I recommend
looking at the CommunityToolkit MVVM package."* `System.Windows.Interactivity`, which it depends
on, is .NET Framework only.

CommunityToolkit.Mvvm maps onto it almost one for one, so the view models kept their shape:

| MvvmLight | CommunityToolkit |
|---|---|
| `ViewModelBase` | `ObservableObject` |
| `RaisePropertyChanged(nameof(X))` | `[ObservableProperty]` source generator |
| `RelayCommand` | `[RelayCommand]` source generator |
| `Messenger.Default` + enum tokens | `WeakReferenceMessenger` + message types |
| `Cleanup()` | `IDisposable` |

The messenger tokens became types in `Infrastructure/Messages.cs`, so the compiler now checks that
a sender and a listener agree — a mistyped enum token used to mean a message that silently went
nowhere. Registrations are weak, so closing a window stops it listening with no explicit cleanup.

### EF6 could have stayed, for the record

EF 6.5.2 (April 2026) added .NET 10 compatibility, and this model is code-first so the
EDMX-designer limitation does not apply. Moving to EF Core was your call to share one data layer
with the web app, not a forced migration.

---

## Running the desktop app

```bash
dotnet build AssetManager.sln
dotnet run --project AssetManager.Wpf
```

Connection string and startup behaviour live in `AssetManager.Wpf/appsettings.json`:

```json
{
  "ConnectionStrings": { "TrackingStore": "Data Source=(LocalDB)\\MSSQLLocalDB;..." },
  "Database": { "MigrateOnStartup": false, "SeedOnStartup": true }
}
```

`|DataDirectory|` in the connection string is expanded at startup, so the existing LocalDB
file-attach string works unchanged. Any setting can be overridden with an environment variable
prefixed `ASSETMANAGER_`.

### Migrations against your existing database — read before enabling

`MigrateOnStartup` is **false** on purpose. Your SQL Server database already has its schema, built
by EF6 migrations, and EF Core knows nothing about EF6's `__MigrationHistory` table. Running
`dotnet ef database update` against it blind would try to create tables that already exist.

To bring it under EF Core migrations, baseline it:

```bash
cd AssetManager.Wpf
dotnet ef migrations add InitialCreate
# Inspect the generated Up() -- it should describe the schema you already have.
dotnet ef migrations script 0 InitialCreate --output baseline.sql   # review, do not run
```

Then insert the migration's row into `__EFMigrationsHistory` by hand so EF Core treats it as
already applied, and only after that set `MigrateOnStartup` to true. Take a backup first — the
app's own File ▸ Export data does exactly that.

The model is mapped to match what EF6 built, including `datetime` rather than EF Core's default
`datetime2`, so a baseline migration should come out close to a no-op. If it does not, that
difference is worth understanding before you apply anything.

---

## Seeding changed, and it affects the Blazor app too

Reference data (units, conversions, the 25 starter inventory items) used to be seeded through EF
Core's `HasData`, which bakes the rows into the model so migrations insert them. That is wrong
here for two reasons: your SQL Server database already contains this data, and two apps sharing a
model should not both be asserting ownership of the same rows.

`SeedData.EnsureSeededAsync` now runs at startup instead, and is **idempotent and additive**:
units and conversions matched on id, inventory items matched on name, nothing ever updated or
deleted. This mirrors what EF6's `Seed` method actually did with `AddOrUpdate`. An item you have
since renamed or re-costed is left alone.

**If you already created an EF Core migration for the Blazor app**, it contains `InsertData` calls
for the seed rows. Regenerate it, or add a migration that drops them — otherwise the next
migration will try to delete rows the seeder is inserting.

---

## Behaviour preserved, and behaviour deliberately changed

### Still preserved (flagged in earlier rounds, still unfixed by choice)

**Batch line costing writes down the remaining need too fast.** In
`BatchCostingService.CalculateLineCost`, each purchase lot credits value for the amount drawn but
decrements the outstanding need by the *full* lot quantity. Where a lot was partly consumed by
earlier batches, the line stops costing early and the batch comes out cheaper than it was. Both
apps now share this behaviour, so they agree with each other and with the old desktop app. The fix
is one identifier; it will change historical batch costs.

**Batch costing ignores the batch date when selecting lots.** Prior consumption is filtered to
earlier batches, but the lots it costs against include orders received *after* the batch.

### Changed on purpose

**Product cost-per-unit now matches the rest of the app.** The old `ViewProducts` screen summed
every batch's cost and divided by every batch's units — a lifetime average. It now uses the shared
service, which walks batches in date order and values what remains against the most recent
production. That is what the old inventory valuation already did, and what the Blazor app shows.
**Expect the Cost Per column to differ from the old app** where a product's cost changed over
time. The old screen was the odd one out.

**The Units column in item history now shows something.** `InventoryItemHistoryViewModel` exposed
`UnitOfMeasureName` as a private property while the XAML bound to it, so that column was always
blank. It is public now.

**A missing intermediate unit conversion no longer crashes.** The EF6 code called `.Single()` when
resolving the alternate-unit bridge, which threw if the row was missing. It reports "Conversion
Error" instead, like the other no-route cases.

**The "New" button on the order window opens Manage Items.** The old `CreateItem` dialog
duplicated the same fields and validation that Manage Items already had. One screen now, and the
lookups refresh automatically when it saves.

**`GridPaginator` was not ported.** It cast every DataGrid row to `List<object>`, which no grid in
this app ever held, so it could not have run without throwing. `InventoryPaginator` — the one the
print command actually used — is ported, with text measured through WPF's `FormattedText` instead
of `System.Drawing`, which drops a Windows-only package used solely to measure strings.

**Window sizes moved to `%APPDATA%\AssetManager\settings.json`.** Previously-remembered sizes are
not carried over; each window reverts to its default once and is then remembered again.

---

## File ▸ Export data

Unchanged in substance: still `BACKUP DATABASE @name TO DISK = @path WITH FORMAT`, still behind a
save dialog. Both the name and the path are passed as parameters, which T-SQL allows for BACKUP.

One thing that was equally true before and is worth knowing: the path is resolved by **SQL
Server**, not by the app. With LocalDB they are the same machine. Against a remote server the file
lands on the server, and the service account needs write access there. The error messages now say
so rather than surfacing a raw SQL error number.

The Blazor app's equivalent is `pg_dump`, on its own `/backup` page.

---

## What to expect on first build

The data and service layer compiled clean before it was split out, and the split was namespace
changes only. New and therefore unverified: the whole WPF project, and the two provider-conditional
lines in `TrackingStore`.

Most likely to need attention, in order:

1. **Package versions.** `CommunityToolkit.Mvvm` 8.4.0, `Microsoft.Xaml.Behaviors.Wpf` 1.1.135,
   `WpfAnimatedGif` 2.0.2, EF Core 10.0.0 — all written without feed access. Run
   `dotnet list package --outdated`, or just `dotnet add package <name>` for each.
2. **Source-generator attributes.** `[ObservableProperty]` on a field named `_foo` generates `Foo`.
   If a generated name collides with something hand-written, that is the error to read first.
   CommunityToolkit 8.4 also warns if a partial class is missing `partial` — every view model here
   has it.
3. **`WpfAnimatedGif` 2.x namespace.** `StartupSplash.xaml` maps it as
   `clr-namespace:WpfAnimatedGif;assembly=WpfAnimatedGif`; the old codeplex XML namespace URI is
   gone. If 2.x exposes a different assembly name, that one line needs updating.
4. **`Database.ProviderName` inside `ConfigureConventions`.** If EF Core 10 objects to reading it
   there, move the DateTime mapping into each app's `OnConfiguring`/options instead.
5. **Nullable warnings** across the WPF project — it has `<Nullable>enable</Nullable>` where the
   original had no nullable context at all. Warnings, not errors.

The `Assets` folder is **not** in this delivery — the icons and `loading.gif` already in your repo
are reused unchanged, and the csproj references them where they are.

---

## Ported file map

| WPF original | Now |
|---|---|
| `Data/*.cs` (13 entities) | `AssetManager.Core/Data/Entities/` |
| `Data/TrackingStore.cs` | `AssetManager.Core/Data/TrackingStore.cs` |
| `Migrations/Configuration.cs` (seed) | `AssetManager.Core/Data/SeedData.cs` |
| `Helpers/InventoryCalculationHelper.cs` | `AssetManager.Core/Services/InventoryCalculationService.cs` |
| costing inline in `BatchViewModelBase` | `AssetManager.Core/Services/BatchCostingService.cs` |
| conversion cascade, inline ×6 | `AssetManager.Core/Services/UnitConversionService.cs` |
| `ItemLookup` / `ProductLookup` / `UnitLookup` | `Infrastructure/LookupCache.cs` |
| `Helpers/MessengerToken.cs` | `Infrastructure/Messages.cs` |
| `Properties/Settings` | `Infrastructure/UserSettingsService.cs` |
| `MainWindow.CommonWindowHandling` | `Infrastructure/WindowManager.cs` |
| `MainWindow.ExportDataMenuItem_Click` | `Infrastructure/DatabaseBackupService.cs` |
| `Add*/Edit*ViewModel` pairs | one view model each, with a nullable id |
| `Helpers/GridPaginator.cs` | not ported (see above) |
| `Views/CreateItem.xaml` | not ported (see above) |
