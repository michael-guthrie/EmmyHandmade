namespace AssetManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBatchOilPercent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.InventoryItem", "IsPercentBatchOil", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.InventoryItem", "IsPercentBatchOil");
        }
    }
}
