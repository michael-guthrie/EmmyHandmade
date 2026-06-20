namespace AssetManager.Helpers
{
    using System;
    using System.Linq;

    internal static class InventoryCalculationHelper
    {
        public static bool TryGetTotalCurrent(Data.InventoryItem i, Data.TrackingStore dbContext, out decimal quantity, out decimal value)
        {
            return TryGetTotalAtDate(i, dbContext, DateTime.Now, out quantity, out value);
        }

        public static bool TryGetTotalAtDate(Data.InventoryItem i, Data.TrackingStore dbContext, DateTime targetDate, out decimal quantity, out decimal value)
        {
            quantity = 0M;
            value = 0M;

            // Find all the used quantity for the item.
            decimal usedItemQty = 0M;

            // Combine all items used in a batch.
            foreach (var bi in dbContext.BatchItems.Where(bi => bi.InventoryItemId == i.Id && bi.Batch.BatchDate <= targetDate).ToList())
            {
                // Check if batch units were same as default.
                decimal adjustedQuantity;
                if (bi.UnitOfMeasureId == i.UnitOfMeasureId)
                {
                    adjustedQuantity = bi.Quantity;
                }
                else
                {
                    // Lookup a conversion to find the common amount used.
                    decimal conversionFactor;
                    var conversion = dbContext.UnitConversions.SingleOrDefault(c => c.UnitFromId == bi.UnitOfMeasureId && c.UnitToId == i.UnitOfMeasureId);
                    if (conversion != null)
                    {
                        conversionFactor = conversion.Factor;
                    }
                    else
                    {
                        // Check if there is a conversion between item groups.
                        if (i.AlternateUnitOfMeasureId.HasValue)
                        {
                            if (bi.UnitOfMeasureId == i.AlternateUnitOfMeasureId)
                            {
                                conversionFactor = i.AlternateUnitOfMeasureFactor.Value;
                            }
                            else
                            {
                                conversion = dbContext.UnitConversions.Single(c => c.UnitFromId == bi.UnitOfMeasureId && c.UnitToId == i.AlternateUnitOfMeasureId);
                                conversionFactor = conversion.Factor * i.AlternateUnitOfMeasureFactor.Value;
                            }
                        }
                        else
                        {
                            // No conversion exists for the item -- can't calculate a valid result.
                            return false;
                        }
                    }
                    adjustedQuantity = bi.Quantity * conversionFactor;
                }
                usedItemQty += adjustedQuantity;
            }

            // Combine all items lost in a batch.
            foreach (var bli in dbContext.BatchLossItems.Where(bli => bli.InventoryItemId == i.Id && bli.Batch.BatchDate <= targetDate).ToList())
            {
                // Check if batch units were same as default.
                decimal adjustedQuantity;
                if (bli.UnitOfMeasureId == i.UnitOfMeasureId)
                {
                    adjustedQuantity = bli.Quantity;
                }
                else
                {
                    // Lookup a conversion to find the common amount used.
                    decimal conversionFactor;
                    var conversion = dbContext.UnitConversions.SingleOrDefault(c => c.UnitFromId == bli.UnitOfMeasureId && c.UnitToId == i.UnitOfMeasureId);
                    if (conversion != null)
                    {
                        conversionFactor = conversion.Factor;
                    }
                    else
                    {
                        // Check if there is a conversion between item groups.
                        if (i.AlternateUnitOfMeasureId.HasValue)
                        {
                            if (bli.UnitOfMeasureId == i.AlternateUnitOfMeasureId)
                            {
                                conversionFactor = i.AlternateUnitOfMeasureFactor.Value;
                            }
                            else
                            {
                                conversion = dbContext.UnitConversions.Single(c => c.UnitFromId == bli.UnitOfMeasureId && c.UnitToId == i.AlternateUnitOfMeasureId);
                                conversionFactor = conversion.Factor * i.AlternateUnitOfMeasureFactor.Value;
                            }
                        }
                        else
                        {
                            // No conversion exists for the item -- can't calculate a valid result.
                            return false;
                        }
                    }
                    adjustedQuantity = bli.Quantity * conversionFactor;
                }
                usedItemQty += adjustedQuantity;
            }

            // Combine all items directly output.
            foreach (var orm in dbContext.OutRecordMaterials.Where(orm => orm.InventoryItemId == i.Id && orm.OutRecord.Date <= targetDate).ToList())
            {
                // Since direct-out materials always use same units, this loop is much easier.
                usedItemQty += orm.Quantity;
            }

            quantity = -1M * usedItemQty;
            value = 0M;

            // Process items by first grouping the same item together, then add according to date.
            foreach (var oi in dbContext.Orders.Where(o => o.DateReceived <= targetDate).OrderBy(o => o.DateReceived).SelectMany(o => o.OrderItems).Where(oi => oi.InventoryItemId == i.Id).ToList())
            {
                // Quantity and unit price may need to be adjusted if the order used different units.
                decimal adjustedQuantity, adjustedUnitPrice;
                if (oi.UnitOfMeasureId == oi.InventoryItem.UnitOfMeasureId)
                {
                    adjustedQuantity = oi.Quantity;
                    adjustedUnitPrice = oi.UnitPrice;
                }
                else
                {
                    decimal conversionFactor;
                    var conversion = dbContext.UnitConversions.SingleOrDefault(c => c.UnitFromId == oi.UnitOfMeasureId && c.UnitToId == i.UnitOfMeasureId);
                    if (conversion != null)
                    {
                        conversionFactor = conversion.Factor;
                    }
                    else
                    {
                        // Check if there is a conversion between item groups.
                        if (i.AlternateUnitOfMeasureId.HasValue)
                        {
                            if (oi.UnitOfMeasureId == i.AlternateUnitOfMeasureId)
                            {
                                conversionFactor = i.AlternateUnitOfMeasureFactor.Value;
                            }
                            else
                            {
                                conversion = dbContext.UnitConversions.Single(c => c.UnitFromId == oi.UnitOfMeasureId && c.UnitToId == i.AlternateUnitOfMeasureId);
                                conversionFactor = conversion.Factor * i.AlternateUnitOfMeasureFactor.Value;
                            }
                        }
                        else
                        {
                            // No conversion exists for the item -- can't calculate a valid result.
                            return false;
                        }
                    }
                    adjustedQuantity = oi.Quantity * conversionFactor;
                    adjustedUnitPrice = oi.UnitPrice / conversionFactor;
                }

                // Increase the quantity according to the order and increase value if applicable.
                quantity += adjustedQuantity;
                if (quantity > 0M)
                {
                    value += Math.Min(quantity, adjustedQuantity) * adjustedUnitPrice;
                }
            }
            return true;
        }

        public static void GetTotalCurrent(Data.BatchProduct p, Data.TrackingStore dbContext, out decimal producedToDate, out decimal outToDate, out decimal unitValue)
        {
            GetTotalAtDate(p, dbContext, DateTime.Now, out producedToDate, out outToDate, out unitValue);
        }

        public static void GetTotalAtDate(Data.BatchProduct p, Data.TrackingStore dbContext, DateTime targetDate, out decimal totalProduced, out decimal totalOut, out decimal remainingValue)
        {
            totalProduced = 0M;
            remainingValue = 0M;

            // Find all the out quantity for the item.
            totalOut = dbContext.OutRecordProducts.Where(orp => orp.BatchProductId == p.Id && orp.OutRecord.Date <= targetDate).Sum(orp => (decimal?)orp.Quantity) ?? 0M;

            // Combine all batch records for creating the product.
            foreach (var b in dbContext.Batches.Where(b => b.ProductId == p.Id && b.BatchDate <= targetDate && b.UnitsProduced != null).ToList())
            {
                decimal unitsProduced = b.UnitsProduced ?? 0M;
                decimal cost = b.Cost ?? 0M;
                totalProduced += unitsProduced;
                if (unitsProduced > 0M)
                {
                    remainingValue += Math.Max(0M, Math.Min(totalProduced - totalOut, unitsProduced)) * (cost / unitsProduced);
                }
            }
        }
    }
}
