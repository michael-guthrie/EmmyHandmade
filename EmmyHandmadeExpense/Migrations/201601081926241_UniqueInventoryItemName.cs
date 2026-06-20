namespace AssetManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UniqueInventoryItemName : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.InventoryItem", "Name", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.InventoryItem", new[] { "Name" });
        }
    }
}
