namespace AssetManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBatchProduct : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.OutRecordProduct", "BatchId", "dbo.Batch");
            DropIndex("dbo.OutRecordProduct", new[] { "BatchId" });
            DropPrimaryKey("dbo.OutRecordProduct");
            CreateTable(
                "dbo.BatchProduct",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true);
            
            AddColumn("dbo.Batch", "ProductId", c => c.Int(nullable: false));
            AddColumn("dbo.OutRecordProduct", "BatchProductId", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.OutRecordProduct", new[] { "OutRecordId", "BatchProductId" });
            CreateIndex("dbo.Batch", "ProductId");
            CreateIndex("dbo.OutRecordProduct", "BatchProductId");
            AddForeignKey("dbo.Batch", "ProductId", "dbo.BatchProduct", "Id", cascadeDelete: true);
            AddForeignKey("dbo.OutRecordProduct", "BatchProductId", "dbo.BatchProduct", "Id", cascadeDelete: true);
            DropColumn("dbo.OutRecordProduct", "BatchId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.OutRecordProduct", "BatchId", c => c.Int(nullable: false));
            DropForeignKey("dbo.OutRecordProduct", "BatchProductId", "dbo.BatchProduct");
            DropForeignKey("dbo.Batch", "ProductId", "dbo.BatchProduct");
            DropIndex("dbo.BatchProduct", new[] { "Name" });
            DropIndex("dbo.OutRecordProduct", new[] { "BatchProductId" });
            DropIndex("dbo.Batch", new[] { "ProductId" });
            DropPrimaryKey("dbo.OutRecordProduct");
            DropColumn("dbo.OutRecordProduct", "BatchProductId");
            DropColumn("dbo.Batch", "ProductId");
            DropTable("dbo.BatchProduct");
            AddPrimaryKey("dbo.OutRecordProduct", new[] { "OutRecordId", "BatchId" });
            CreateIndex("dbo.OutRecordProduct", "BatchId");
            AddForeignKey("dbo.OutRecordProduct", "BatchId", "dbo.Batch", "Id", cascadeDelete: true);
        }
    }
}
