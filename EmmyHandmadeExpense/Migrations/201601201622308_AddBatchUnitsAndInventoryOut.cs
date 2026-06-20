namespace AssetManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBatchUnitsAndInventoryOut : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OutRecord",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Date = c.DateTime(nullable: false),
                    Description = c.String(maxLength: 500),
                    TotalSale = c.Decimal(nullable: false, precision: 18, scale: 2),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.OutRecordMaterial",
                c => new
                    {
                        OutRecordId = c.Int(nullable: false),
                        InventoryItemId = c.Int(nullable: false),
                        Quantity = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.OutRecordId, t.InventoryItemId })
                .ForeignKey("dbo.OutRecord", t => t.OutRecordId, cascadeDelete: true)
                .Index(t => t.OutRecordId);
            
            CreateTable(
                "dbo.OutRecordProduct",
                c => new
                    {
                        OutRecordId = c.Int(nullable: false),
                        BatchId = c.Int(nullable: false),
                        Quantity = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => new { t.OutRecordId, t.BatchId })
                .ForeignKey("dbo.OutRecord", t => t.OutRecordId, cascadeDelete: true)
                .Index(t => t.OutRecordId);
            
            AddColumn("dbo.Batch", "Cost", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.Batch", "UnitsProduced", c => c.Decimal(precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.OutRecordProduct", "OutRecordId", "dbo.OutRecord");
            DropForeignKey("dbo.OutRecordMaterial", "OutRecordId", "dbo.OutRecord");
            DropIndex("dbo.OutRecordProduct", new[] { "OutRecordId" });
            DropIndex("dbo.OutRecordMaterial", new[] { "OutRecordId" });
            DropColumn("dbo.Batch", "UnitsProduced");
            DropColumn("dbo.Batch", "Cost");
            DropTable("dbo.OutRecordProduct");
            DropTable("dbo.OutRecord");
            DropTable("dbo.OutRecordMaterial");
        }
    }
}
