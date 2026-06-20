namespace AssetManager.Data
{
    using System.Data.Entity;

    public partial class TrackingStore : DbContext
    {
        public const string DesignTimeConnection = "name=AssetManager.Properties.Settings.TrackingStoreConnectionStringDesignTime";

        public static string ApplicationInstanceConnectionString { get; set; } = DesignTimeConnection;

        public TrackingStore()
            : base(ApplicationInstanceConnectionString)
        {
        }

        public TrackingStore(System.Data.Common.DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {

        }

        public virtual DbSet<Batch> Batches { get; set; }
        public virtual DbSet<BatchItem> BatchItems { get; set; }
        public virtual DbSet<BatchLossItem> BatchLossItems { get; set; }
        public virtual DbSet<BatchProduct> BatchProducts { get; set; }
        public virtual DbSet<InventoryItem> InventoryItems { get; set; }
        public virtual DbSet<MiscellaneousExpense> MiscellaneousExpenses { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }
        public virtual DbSet<OutRecord> OutRecords { get; set; }
        public virtual DbSet<OutRecordMaterial> OutRecordMaterials { get; set; }
        public virtual DbSet<OutRecordProduct> OutRecordProducts { get; set; }
        public virtual DbSet<UnitConversion> UnitConversions { get; set; }
        public virtual DbSet<UnitOfMeasure> UnitsOfMeasure { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Batch>()
                .HasMany(e => e.BatchItems)
                .WithRequired(e => e.Batch)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Batch>()
                .HasMany(e => e.BatchLossItems)
                .WithRequired(e => e.Batch)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<BatchItem>()
                .Property(e => e.Quantity)
                .HasPrecision(18, 8);

            modelBuilder.Entity<BatchLossItem>()
                .Property(e => e.Quantity)
                .HasPrecision(18, 8);

            modelBuilder.Entity<InventoryItem>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<InventoryItem>()
                .Property(e => e.AlternateUnitOfMeasureFactor)
                .HasPrecision(18, 10);

            modelBuilder.Entity<InventoryItem>()
                .HasMany(e => e.BatchItems)
                .WithRequired(e => e.InventoryItem)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<InventoryItem>()
                .HasMany(e => e.BatchLossItems)
                .WithRequired(e => e.InventoryItem)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<InventoryItem>()
                .HasMany(e => e.OrderItems)
                .WithRequired(e => e.InventoryItem)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Order>()
                .Property(e => e.Source)
                .IsUnicode(false);

            modelBuilder.Entity<Order>()
                .Property(e => e.OrderNumber)
                .IsUnicode(false);

            modelBuilder.Entity<Order>()
                .Property(e => e.Link)
                .IsUnicode(false);

            modelBuilder.Entity<Order>()
                .Property(e => e.Tax)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Order>()
                .Property(e => e.Shipping)
                .HasPrecision(18, 4);

            modelBuilder.Entity<Order>()
                .HasMany(e => e.OrderItems)
                .WithRequired(e => e.Order)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<OrderItem>()
                .Property(e => e.UnitPrice)
                .HasPrecision(18, 6);

            modelBuilder.Entity<OrderItem>()
                .Property(e => e.Quantity)
                .HasPrecision(18, 6);

            modelBuilder.Entity<UnitConversion>()
                .Property(e => e.Factor)
                .HasPrecision(18, 8);

            modelBuilder.Entity<UnitOfMeasure>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<UnitOfMeasure>()
                .HasMany(e => e.BatchItems)
                .WithRequired(e => e.UnitOfMeasure)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UnitOfMeasure>()
                .HasMany(e => e.BatchLossItems)
                .WithRequired(e => e.UnitOfMeasure)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UnitOfMeasure>()
                .HasMany(e => e.InventoryItems)
                .WithRequired(e => e.UnitOfMeasure)
                .HasForeignKey(e => e.UnitOfMeasureId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UnitOfMeasure>()
                .HasMany(e => e.InventoryItemAlternates)
                .WithOptional(e => e.AlternateUnitOfMeasure)
                .HasForeignKey(e => e.AlternateUnitOfMeasureId);

            modelBuilder.Entity<UnitOfMeasure>()
                .HasMany(e => e.OrderItems)
                .WithRequired(e => e.UnitOfMeasure)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UnitOfMeasure>()
                .HasMany(e => e.UnitFromConversions)
                .WithRequired(e => e.UnitOfMeasureFrom)
                .HasForeignKey(e => e.UnitFromId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UnitOfMeasure>()
                .HasMany(e => e.UnitToConversions)
                .WithRequired(e => e.UnitOfMeasureTo)
                .HasForeignKey(e => e.UnitToId)
                .WillCascadeOnDelete(false);
        }
    }
}
