using AssetManager.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AssetManager.Core.Data;

/// <summary>
/// EF Core port of the WPF app's EF6 <c>TrackingStore</c> context, shared by the desktop and web
/// front ends.
///
/// Table names, column names and decimal precisions are identical to the EF6 model, so this maps
/// onto an existing SQL Server database created by the old app without reshaping anything.
///
/// <para>The model is provider-neutral. Each app supplies its own provider — the WPF app uses SQL
/// Server, the Blazor app PostgreSQL — and the only provider-specific decision, how
/// <see cref="DateTime"/> maps to a column type, is made from
/// <see cref="DatabaseFacade.ProviderName"/> so neither provider package has to be referenced
/// here.</para>
///
/// <para>Note that PostgreSQL folds unquoted identifiers to lower case; EF Core always quotes
/// them, so the original PascalCase names survive, but hand-written SQL must quote them too
/// (<c>select * from "Order"</c>, not <c>select * from Order</c>).</para>
/// </summary>
public class TrackingStore : DbContext
{
    public const string NpgsqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";
    public const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

    public TrackingStore(DbContextOptions<TrackingStore> options) : base(options) { }

    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchItem> BatchItems => Set<BatchItem>();
    public DbSet<BatchLossItem> BatchLossItems => Set<BatchLossItem>();
    public DbSet<BatchProduct> BatchProducts => Set<BatchProduct>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<MiscellaneousExpense> MiscellaneousExpenses => Set<MiscellaneousExpense>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OutRecord> OutRecords => Set<OutRecord>();
    public DbSet<OutRecordMaterial> OutRecordMaterials => Set<OutRecordMaterial>();
    public DbSet<OutRecordProduct> OutRecordProducts => Set<OutRecordProduct>();
    public DbSet<UnitConversion> UnitConversions => Set<UnitConversion>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Every DateTime in this model is a business date (batch date, order date, expense date),
        // not an instant, and the column type has to say so differently per provider.
        var columnType = Database.ProviderName switch
        {
            // Npgsql defaults DateTime to "timestamp with time zone", which rejects any DateTime
            // whose Kind is not Utc and shifts values on read.
            NpgsqlProvider => "timestamp without time zone",

            // EF Core defaults to datetime2 on SQL Server, but the existing database was created
            // by EF6, which used datetime. Matching it keeps this model mapping cleanly onto the
            // tables that are already there.
            SqlServerProvider => "datetime",

            _ => null,
        };

        if (columnType is null) return;

        builder.Properties<DateTime>().HaveColumnType(columnType);
        builder.Properties<DateTime?>().HaveColumnType(columnType);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUnits(modelBuilder);
        ConfigureInventory(modelBuilder);
        ConfigureBatches(modelBuilder);
        ConfigureOrders(modelBuilder);
        ConfigureOutput(modelBuilder);
        ConfigureExpenses(modelBuilder);

        // Reference data is NOT seeded through HasData. See SeedData for why.
    }

    private static void ConfigureUnits(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UnitOfMeasure>(e =>
        {
            e.Property(u => u.Name).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<UnitConversion>(e =>
        {
            e.HasKey(c => new { c.UnitFromId, c.UnitToId });
            e.Property(c => c.Factor).HasPrecision(18, 8);

            e.HasOne(c => c.UnitOfMeasureFrom)
             .WithMany(u => u.UnitFromConversions)
             .HasForeignKey(c => c.UnitFromId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.UnitOfMeasureTo)
             .WithMany(u => u.UnitToConversions)
             .HasForeignKey(c => c.UnitToId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.Property(i => i.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(i => i.Name).IsUnique();
            e.Property(i => i.AlternateUnitOfMeasureFactor).HasPrecision(18, 10);

            e.HasOne(i => i.UnitOfMeasure)
             .WithMany(u => u.InventoryItems)
             .HasForeignKey(i => i.UnitOfMeasureId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.AlternateUnitOfMeasure)
             .WithMany(u => u.InventoryItemAlternates)
             .HasForeignKey(i => i.AlternateUnitOfMeasureId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BatchProduct>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(p => p.Name).IsUnique();
        });
    }

    private static void ConfigureBatches(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Batch>(e =>
        {
            e.Property(b => b.Description).HasMaxLength(500);
            e.Property(b => b.Cost).HasPrecision(18, 4);
            e.Property(b => b.UnitsProduced).HasPrecision(18, 6);

            e.HasOne(b => b.Product)
             .WithMany(p => p.Batches)
             .HasForeignKey(b => b.ProductId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(b => b.BatchItems)
             .WithOne(bi => bi.Batch!)
             .HasForeignKey(bi => bi.BatchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(b => b.BatchLossItems)
             .WithOne(bli => bli.Batch!)
             .HasForeignKey(bli => bli.BatchId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(b => b.BatchDate);
        });

        modelBuilder.Entity<BatchItem>(e =>
        {
            e.Property(bi => bi.Quantity).HasPrecision(18, 8);

            e.HasOne(bi => bi.InventoryItem)
             .WithMany(i => i.BatchItems)
             .HasForeignKey(bi => bi.InventoryItemId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(bi => bi.UnitOfMeasure)
             .WithMany(u => u.BatchItems)
             .HasForeignKey(bi => bi.UnitOfMeasureId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BatchLossItem>(e =>
        {
            e.Property(bli => bli.Quantity).HasPrecision(18, 8);

            e.HasOne(bli => bli.InventoryItem)
             .WithMany(i => i.BatchLossItems)
             .HasForeignKey(bli => bli.InventoryItemId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(bli => bli.UnitOfMeasure)
             .WithMany(u => u.BatchLossItems)
             .HasForeignKey(bli => bli.UnitOfMeasureId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.Source).HasMaxLength(50);
            e.Property(o => o.OrderNumber).HasMaxLength(50);
            e.Property(o => o.Link).HasMaxLength(500);
            e.Property(o => o.Tax).HasPrecision(18, 4);
            e.Property(o => o.Shipping).HasPrecision(18, 4);
            e.Property(o => o.Discount).HasPrecision(18, 4);

            e.HasMany(o => o.OrderItems)
             .WithOne(oi => oi.Order!)
             .HasForeignKey(oi => oi.OrderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(o => o.DatePlaced);
            e.HasIndex(o => o.DateReceived);

            e.Ignore(o => o.Total);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(oi => oi.UnitPrice).HasPrecision(18, 6);
            e.Property(oi => oi.Quantity).HasPrecision(18, 6);

            e.HasOne(oi => oi.InventoryItem)
             .WithMany(i => i.OrderItems)
             .HasForeignKey(oi => oi.InventoryItemId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(oi => oi.UnitOfMeasure)
             .WithMany(u => u.OrderItems)
             .HasForeignKey(oi => oi.UnitOfMeasureId)
             .OnDelete(DeleteBehavior.Restrict);

            e.Ignore(oi => oi.ExtendedPrice);
        });
    }

    private static void ConfigureOutput(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutRecord>(e =>
        {
            e.Property(o => o.Description).HasMaxLength(500);
            e.Property(o => o.TotalSale).HasPrecision(18, 4);
            e.HasIndex(o => o.Date);
        });

        modelBuilder.Entity<OutRecordMaterial>(e =>
        {
            e.HasKey(m => new { m.OutRecordId, m.InventoryItemId });
            e.Property(m => m.Quantity).HasPrecision(18, 8);

            // Lines are owned by the record: deleting an OutRecord removes its lines, which is
            // how the WPF edit screen saves a modified record.
            e.HasOne(m => m.OutRecord)
             .WithMany(o => o.Materials)
             .HasForeignKey(m => m.OutRecordId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.InventoryItem)
             .WithMany(i => i.OutRecordMaterials)
             .HasForeignKey(m => m.InventoryItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OutRecordProduct>(e =>
        {
            e.HasKey(p => new { p.OutRecordId, p.BatchProductId });
            e.Property(p => p.Quantity).HasPrecision(18, 8);

            e.HasOne(p => p.OutRecord)
             .WithMany(o => o.Products)
             .HasForeignKey(p => p.OutRecordId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.BatchProduct)
             .WithMany(bp => bp.OutRecordProducts)
             .HasForeignKey(p => p.BatchProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureExpenses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MiscellaneousExpense>(e =>
        {
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 4);
            e.HasIndex(x => x.DateOfExpense);
        });
    }
}
