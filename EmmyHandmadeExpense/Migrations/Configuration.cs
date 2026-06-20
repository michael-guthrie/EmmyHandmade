namespace AssetManager.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<AssetManager.Data.TrackingStore>
    {
        public class SeedingErrorEventArgs : EventArgs
        {
            public string SeedAction { get; private set; }

            public Exception Error { get; private set; }

            public SeedingErrorEventArgs(string seedAction, Exception error) { SeedAction = seedAction; Error = error; }
        }

        public event EventHandler<SeedingErrorEventArgs> SeedingError;

        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(AssetManager.Data.TrackingStore context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //

            try
            {
                context.UnitsOfMeasure.AddOrUpdate(
                    new Data.UnitOfMeasure { Id = 101, Name = "fl oz", UnitType = 1 },
                    new Data.UnitOfMeasure { Id = 102, Name = "oz", UnitType = 2 },
                    new Data.UnitOfMeasure { Id = 103, Name = "gal", UnitType = 1 },
                    new Data.UnitOfMeasure { Id = 104, Name = "lb", UnitType = 2 },
                    new Data.UnitOfMeasure { Id = 105, Name = "ea", UnitType = 0 },
                    new Data.UnitOfMeasure { Id = 106, Name = "yd", UnitType = 3 },
                    new Data.UnitOfMeasure { Id = 107, Name = "ft", UnitType = 3 },
                    new Data.UnitOfMeasure { Id = 108, Name = "m", UnitType = 3 },
                    new Data.UnitOfMeasure { Id = 109, Name = "in", UnitType = 3 },
                    new Data.UnitOfMeasure { Id = 110, Name = "cm", UnitType = 3 },
                    new Data.UnitOfMeasure { Id = 111, Name = "L", UnitType = 1 },
                    new Data.UnitOfMeasure { Id = 112, Name = "mL", UnitType = 1 },
                    new Data.UnitOfMeasure { Id = 113, Name = "pack", UnitType = 1 }
                );
            }
            catch (Exception ex)
            {
                if (!OnSeedingError("Error seeding units of measure.", ex))
                {
                    throw;
                }
            }

            try
            {
                context.UnitConversions.AddOrUpdate(
                    new Data.UnitConversion { UnitFromId = 103, UnitToId = 101, Factor = 128M },
                    new Data.UnitConversion { UnitFromId = 111, UnitToId = 101, Factor = 33.81402270M },
                    new Data.UnitConversion { UnitFromId = 112, UnitToId = 101, Factor = 0.03814023M },
                    new Data.UnitConversion { UnitFromId = 104, UnitToId = 102, Factor = 16M },
                    new Data.UnitConversion { UnitFromId = 101, UnitToId = 103, Factor = 0.00781250M },
                    new Data.UnitConversion { UnitFromId = 111, UnitToId = 103, Factor = 0.26417205M },
                    new Data.UnitConversion { UnitFromId = 112, UnitToId = 103, Factor = 0.00026417M },
                    new Data.UnitConversion { UnitFromId = 102, UnitToId = 104, Factor = 0.06250000M },
                    new Data.UnitConversion { UnitFromId = 107, UnitToId = 106, Factor = 0.33333333M },
                    new Data.UnitConversion { UnitFromId = 108, UnitToId = 106, Factor = 1.09361330M },
                    new Data.UnitConversion { UnitFromId = 109, UnitToId = 106, Factor = 0.02777778M },
                    new Data.UnitConversion { UnitFromId = 110, UnitToId = 106, Factor = 0.01093613M },
                    new Data.UnitConversion { UnitFromId = 106, UnitToId = 107, Factor = 3.00000000M },
                    new Data.UnitConversion { UnitFromId = 108, UnitToId = 107, Factor = 3.28083990M },
                    new Data.UnitConversion { UnitFromId = 110, UnitToId = 107, Factor = 0.03280840M },
                    new Data.UnitConversion { UnitFromId = 106, UnitToId = 108, Factor = 0.91440000M },
                    new Data.UnitConversion { UnitFromId = 107, UnitToId = 108, Factor = 0.30480000M },
                    new Data.UnitConversion { UnitFromId = 109, UnitToId = 108, Factor = 0.02540000M },
                    new Data.UnitConversion { UnitFromId = 110, UnitToId = 108, Factor = 0.01000000M },
                    new Data.UnitConversion { UnitFromId = 106, UnitToId = 109, Factor = 36.00000000M },
                    new Data.UnitConversion { UnitFromId = 108, UnitToId = 109, Factor = 39.37007870M },
                    new Data.UnitConversion { UnitFromId = 110, UnitToId = 109, Factor = 0.39370079M },
                    new Data.UnitConversion { UnitFromId = 106, UnitToId = 110, Factor = 91.44000000M },
                    new Data.UnitConversion { UnitFromId = 107, UnitToId = 110, Factor = 30.48000000M },
                    new Data.UnitConversion { UnitFromId = 108, UnitToId = 110, Factor = 100.00000000M },
                    new Data.UnitConversion { UnitFromId = 109, UnitToId = 110, Factor = 2.54000000M },
                    new Data.UnitConversion { UnitFromId = 101, UnitToId = 111, Factor = 0.02957353M },
                    new Data.UnitConversion { UnitFromId = 103, UnitToId = 111, Factor = 3.78541178M },
                    new Data.UnitConversion { UnitFromId = 101, UnitToId = 112, Factor = 29.57352960M },
                    new Data.UnitConversion { UnitFromId = 103, UnitToId = 112, Factor = 3785.41178000M },
                    new Data.UnitConversion { UnitFromId = 111, UnitToId = 112, Factor = 1000.00000000M }
                );
            }
            catch (Exception ex)
            {
                if (!OnSeedingError("Error seeding unit conversions.", ex))
                {
                    throw;
                }
            }

            try
            {
                context.InventoryItems.AddOrUpdate(
                    i => i.Name,
                    new Data.InventoryItem { Name = "Lye", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Coconut Oil", UnitOfMeasureId = 102, AlternateUnitOfMeasureId = 103, AlternateUnitOfMeasureFactor = 123.1277420000M, IsPercentBatchOil = true },
                    new Data.InventoryItem { Name = "Olive Oil", UnitOfMeasureId = 102, IsPercentBatchOil = true },
                    new Data.InventoryItem { Name = "Shea Butter", UnitOfMeasureId = 102, IsPercentBatchOil = true },
                    new Data.InventoryItem { Name = "Palm Oil", UnitOfMeasureId = 102, IsPercentBatchOil = true },
                    new Data.InventoryItem { Name = "Canola Oil", UnitOfMeasureId = 102, IsPercentBatchOil = true },
                    new Data.InventoryItem { Name = "Castor Oil", UnitOfMeasureId = 102, IsPercentBatchOil = true },
                    new Data.InventoryItem { Name = "Sweet Almond Oil", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Avacado Oil", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Cocoa Butter", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Gift Box 4x4x4", UnitOfMeasureId = 105 },
                    new Data.InventoryItem { Name = "Gift Box 6x6x6", UnitOfMeasureId = 105 },
                    new Data.InventoryItem { Name = "Distilled Water", UnitOfMeasureId = 102, AlternateUnitOfMeasureId = 103, AlternateUnitOfMeasureFactor = 133.980134M },
                    new Data.InventoryItem { Name = "Epsom Salt", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Dead Sea Salt", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Pink Himalaya Salt", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Sea Salt", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Lavender Buds", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Dried Calendula Flower", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Dried Rose Petals", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Beeswax", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Lip Balm Tube", UnitOfMeasureId = 105 },
                    new Data.InventoryItem { Name = "Dropper", UnitOfMeasureId = 105 },
                    new Data.InventoryItem { Name = "Orange Essential Oil", UnitOfMeasureId = 102 },
                    new Data.InventoryItem { Name = "Lavender Essential Oil", UnitOfMeasureId = 102 }
                );
            }
            catch (Exception ex)
            {
                if (!OnSeedingError("Error seeding initial inventory items.", ex))
                {
                    throw;
                }
            }
        }

        private bool OnSeedingError(string seedAction, Exception error)
        {
            if (SeedingError != null)
            {
                SeedingError(this, new SeedingErrorEventArgs(seedAction, error));
                return true;
            }
            return false;
        }
    }
}
