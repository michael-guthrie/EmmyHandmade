namespace AssetManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OutItemFKs : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.OutRecordMaterial", "InventoryItemId");
            CreateIndex("dbo.OutRecordProduct", "BatchId");
            AddForeignKey("dbo.OutRecordMaterial", "InventoryItemId", "dbo.InventoryItem", "Id", cascadeDelete: true);
            AddForeignKey("dbo.OutRecordProduct", "BatchId", "dbo.Batch", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.OutRecordProduct", "BatchId", "dbo.Batch");
            DropForeignKey("dbo.OutRecordMaterial", "InventoryItemId", "dbo.InventoryItem");
            DropIndex("dbo.OutRecordProduct", new[] { "BatchId" });
            DropIndex("dbo.OutRecordMaterial", new[] { "InventoryItemId" });
        }
    }
}
