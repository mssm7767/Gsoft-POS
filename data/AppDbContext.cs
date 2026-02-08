using GSoftPosNew.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GSoftPosNew.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<ItemModel> Items { get; set; }
        public DbSet<ItemBarcodeModel> ItemBarcodes { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierPayment> SupplierPayments { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerPayment> CustomerPayments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Item> Products { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
        public DbSet<Expense> Expenses { get; set; }

        public DbSet<Unit> Units { get; set; }
        public DbSet<ShopSetting> ShopSettings { get; set; }

        public DbSet<IngredientCategory> IngredientCategories { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<ItemIngredient> ItemIngredients { get; set; }
        public DbSet<IngredientPurchase> IngredientPurchases { get; set; }
        public DbSet<IngredientPurchaseItem> IngredientPurchaseItems { get; set; }

        public DbSet<StockAdjustment> StockAdjustments { get; set; }
        public DbSet<StockAdjustmentItem> StockAdjustmentItems { get; set; }
        public DbSet<CustomerVanStock> CustomerVanStocks { get; set; }
        public DbSet<VanStockSale> VanStockSales { get; set; }
        public DbSet<PosTable> PosTables { get; set; }
        public DbSet<SoftwareLicense> SoftwareLicense { get; set; }
        public DbSet<MultiBarcodes> MultiBarcodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================
            // Relationships
            // ============================
            modelBuilder.Entity<Sale>()
                .HasMany(s => s.SaleItems)
                .WithOne(si => si.Sale)
                .HasForeignKey(si => si.SaleId);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Payment)
                .WithOne(p => p.Sale)
                .HasForeignKey<Payment>(p => p.SaleId);

            modelBuilder.Entity<SaleItem>()
                .HasOne(si => si.Item)
                .WithMany()
                .HasForeignKey(si => si.ItemId);

            modelBuilder.Entity<ItemIngredient>()
                .HasOne(ii => ii.Item)
                .WithMany(i => i.ItemIngredients)
                .HasForeignKey(ii => ii.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================
            // ✅ UNIQUE INDEXES (IMPORTANT)
            // ============================

            // ✅ Item Code / Barcode must be unique
            modelBuilder.Entity<ItemModel>()
                .HasIndex(x => x.ItemCode)
                .IsUnique();

            // Optional (Recommended): length limit
            modelBuilder.Entity<ItemModel>()
                .Property(x => x.ItemCode)
                .HasMaxLength(50);

            // ✅ OPTIONAL: Item Name unique (agar aap chaho)
            // Agar same name allow karna hai to isko comment hi rehne dein
            /*
            modelBuilder.Entity<ItemModel>()
                .HasIndex(x => x.ItemName)
                .IsUnique();

            modelBuilder.Entity<ItemModel>()
                .Property(x => x.ItemName)
                .HasMaxLength(200);
            */

            // ✅ MultiBarcodes: Barcode unique (GLOBAL) => same barcode kisi aur item me repeat nahi hoga
            modelBuilder.Entity<MultiBarcodes>()
                .HasIndex(x => x.Barcode)
                .IsUnique();

            modelBuilder.Entity<MultiBarcodes>()
                .Property(x => x.Barcode)
                .HasMaxLength(80);

            // ✅ OPTION B (Per Item Unique) - agar aap global unique nahi chahte:
            // Upar wala (HasIndex(x => x.Barcode).IsUnique()) comment kar ke ye enable kar den:
            /*
            modelBuilder.Entity<MultiBarcodes>()
                .HasIndex(x => new { x.ItemId, x.Barcode })
                .IsUnique();
            */
        }
    }
}
