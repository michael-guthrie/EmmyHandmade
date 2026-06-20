namespace AssetManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Batch",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    BatchDate = c.DateTime(nullable: false),
                    Description = c.String(maxLength: 500),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.BatchItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    BatchId = c.Int(nullable: false),
                    InventoryItemId = c.Int(nullable: false),
                    Quantity = c.Decimal(nullable: false, precision: 18, scale: 8),
                    UnitOfMeasureId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.UnitOfMeasure", t => t.UnitOfMeasureId)
                .ForeignKey("dbo.InventoryItem", t => t.InventoryItemId)
                .ForeignKey("dbo.Batch", t => t.BatchId)
                .Index(t => t.BatchId)
                .Index(t => t.InventoryItemId)
                .Index(t => t.UnitOfMeasureId);

            CreateTable(
                "dbo.InventoryItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 50, unicode: false),
                    UnitOfMeasureId = c.Int(nullable: false),
                    AlternateUnitOfMeasureFactor = c.Decimal(precision: 18, scale: 10),
                    AlternateUnitOfMeasureId = c.Int(),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.UnitOfMeasure", t => t.AlternateUnitOfMeasureId)
                .ForeignKey("dbo.UnitOfMeasure", t => t.UnitOfMeasureId)
                .Index(t => t.UnitOfMeasureId)
                .Index(t => t.AlternateUnitOfMeasureId);

            CreateTable(
                "dbo.UnitOfMeasure",
                c => new
                {
                    Id = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 50, unicode: false),
                    UnitType = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.BatchLossItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    BatchId = c.Int(nullable: false),
                    InventoryItemId = c.Int(nullable: false),
                    Quantity = c.Decimal(nullable: false, precision: 18, scale: 8),
                    UnitOfMeasureId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.UnitOfMeasure", t => t.UnitOfMeasureId)
                .ForeignKey("dbo.InventoryItem", t => t.InventoryItemId)
                .ForeignKey("dbo.Batch", t => t.BatchId)
                .Index(t => t.BatchId)
                .Index(t => t.InventoryItemId)
                .Index(t => t.UnitOfMeasureId);

            CreateTable(
                "dbo.OrderItem",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    OrderId = c.Int(nullable: false),
                    InventoryItemId = c.Int(nullable: false),
                    UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 6),
                    Quantity = c.Decimal(nullable: false, precision: 18, scale: 6),
                    UnitOfMeasureId = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Order", t => t.OrderId)
                .ForeignKey("dbo.UnitOfMeasure", t => t.UnitOfMeasureId)
                .ForeignKey("dbo.InventoryItem", t => t.InventoryItemId)
                .Index(t => t.OrderId)
                .Index(t => t.InventoryItemId)
                .Index(t => t.UnitOfMeasureId);

            CreateTable(
                "dbo.Order",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Source = c.String(maxLength: 50, unicode: false),
                    OrderNumber = c.String(maxLength: 50, unicode: false),
                    Link = c.String(maxLength: 500, unicode: false),
                    Tax = c.Decimal(precision: 18, scale: 4),
                    Shipping = c.Decimal(precision: 18, scale: 4),
                    DatePlaced = c.DateTime(nullable: false),
                    DateReceived = c.DateTime(),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.UnitConversion",
                c => new
                {
                    UnitFromId = c.Int(nullable: false),
                    UnitToId = c.Int(nullable: false),
                    Factor = c.Decimal(nullable: false, precision: 18, scale: 8),
                })
                .PrimaryKey(t => new { t.UnitFromId, t.UnitToId })
                .ForeignKey("dbo.UnitOfMeasure", t => t.UnitFromId)
                .ForeignKey("dbo.UnitOfMeasure", t => t.UnitToId)
                .Index(t => t.UnitFromId)
                .Index(t => t.UnitToId);
        }

        public override void Down()
        {
            DropForeignKey("dbo.BatchLossItem", "BatchId", "dbo.Batch");
            DropForeignKey("dbo.BatchItem", "BatchId", "dbo.Batch");
            DropForeignKey("dbo.OrderItem", "InventoryItemId", "dbo.InventoryItem");
            DropForeignKey("dbo.BatchLossItem", "InventoryItemId", "dbo.InventoryItem");
            DropForeignKey("dbo.BatchItem", "InventoryItemId", "dbo.InventoryItem");
            DropForeignKey("dbo.UnitConversion", "UnitToId", "dbo.UnitOfMeasure");
            DropForeignKey("dbo.UnitConversion", "UnitFromId", "dbo.UnitOfMeasure");
            DropForeignKey("dbo.OrderItem", "UnitOfMeasureId", "dbo.UnitOfMeasure");
            DropForeignKey("dbo.OrderItem", "OrderId", "dbo.Order");
            DropForeignKey("dbo.InventoryItem", "UnitOfMeasureId", "dbo.UnitOfMeasure");
            DropForeignKey("dbo.InventoryItem", "AlternateUnitOfMeasureId", "dbo.UnitOfMeasure");
            DropForeignKey("dbo.BatchLossItem", "UnitOfMeasureId", "dbo.UnitOfMeasure");
            DropForeignKey("dbo.BatchItem", "UnitOfMeasureId", "dbo.UnitOfMeasure");
            DropIndex("dbo.UnitConversion", new[] { "UnitToId" });
            DropIndex("dbo.UnitConversion", new[] { "UnitFromId" });
            DropIndex("dbo.OrderItem", new[] { "UnitOfMeasureId" });
            DropIndex("dbo.OrderItem", new[] { "InventoryItemId" });
            DropIndex("dbo.OrderItem", new[] { "OrderId" });
            DropIndex("dbo.BatchLossItem", new[] { "UnitOfMeasureId" });
            DropIndex("dbo.BatchLossItem", new[] { "InventoryItemId" });
            DropIndex("dbo.BatchLossItem", new[] { "BatchId" });
            DropIndex("dbo.InventoryItem", new[] { "AlternateUnitOfMeasureId" });
            DropIndex("dbo.InventoryItem", new[] { "UnitOfMeasureId" });
            DropIndex("dbo.BatchItem", new[] { "UnitOfMeasureId" });
            DropIndex("dbo.BatchItem", new[] { "InventoryItemId" });
            DropIndex("dbo.BatchItem", new[] { "BatchId" });
            DropTable("dbo.UnitConversion");
            DropTable("dbo.Order");
            DropTable("dbo.OrderItem");
            DropTable("dbo.BatchLossItem");
            DropTable("dbo.UnitOfMeasure");
            DropTable("dbo.InventoryItem");
            DropTable("dbo.BatchItem");
            DropTable("dbo.Batch");
        }
    }
}
