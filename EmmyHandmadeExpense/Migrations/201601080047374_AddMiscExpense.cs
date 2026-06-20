namespace AssetManager.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMiscExpense : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MiscellaneousExpense",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DateOfExpense = c.DateTime(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 8),
                        Description = c.String(nullable: false, maxLength: 500),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.DateOfExpense);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.MiscellaneousExpense", new[] { "DateOfExpense" });
            DropTable("dbo.MiscellaneousExpense");
        }
    }
}
