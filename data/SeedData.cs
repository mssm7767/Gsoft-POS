using GSoftPosNew.Data;
using GSoftPosNew.Models;

namespace GSoftPosNew.data
{
    public static class SeedData
    {
        public static void Initialize(AppDbContext context)
        {
            if (!context.Products.Any())
            {
                context.Products.AddRange(
                    new Item { ItemCode = "ITEM001", ItemName = "Laptop", GenericName = "Electronics", SalePrice = 999.99m, CostPrice = 800.00m, StockQuantity = 10 },
                    new Item { ItemCode = "ITEM002", ItemName = "Mouse", GenericName = "Electronics", SalePrice = 19.99m, CostPrice = 10.00m, StockQuantity = 50 },
                    new Item { ItemCode = "ITEM003", ItemName = "Keyboard", GenericName = "Electronics", SalePrice = 49.99m, CostPrice = 30.00m, StockQuantity = 30 },
                    new Item { ItemCode = "ITEM004", ItemName = "Notebook", GenericName = "Stationery", SalePrice = 2.99m, CostPrice = 1.50m, StockQuantity = 100 },
                    new Item { ItemCode = "ITEM005", ItemName = "Pen", GenericName = "Stationery", SalePrice = 1.49m, CostPrice = 0.50m, StockQuantity = 200 }
                );

                context.SaveChanges();
            }
        }
    }
}
