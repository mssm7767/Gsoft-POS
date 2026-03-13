using System;
using System.Collections.Generic;
using System.Linq;

namespace GSoftPosNew.ViewModels
{
    public class UserSalesDashboardVM
    {
        public DateTime ReportDate { get; set; } = DateTime.Today;
        public List<UserSalesCardVM> Users { get; set; } = new();

        public decimal GrandTotalSale => Users?.Sum(x => x.TotalSale) ?? 0;
        public decimal GrandTotalPayment => Users?.Sum(x => x.TotalPayment) ?? 0;
        public decimal GrandTotalQty => Users?.Sum(x => x.TotalQty) ?? 0;
        public int GrandTotalItems => Users?.Sum(x => x.TotalDistinctItems) ?? 0;
        public decimal GrandTotalDiscount => Users?.Sum(x => x.TotalDiscount) ?? 0;
    }

    public class UserSalesCardVM
    {
        public int UserId { get; set; }
        public string CashierId { get; set; } = "";
        public string UserName { get; set; } = "";
        public decimal TotalQty { get; set; }
        public decimal TotalSale { get; set; }
        public decimal TotalPayment { get; set; }
        public decimal TotalDiscount { get; set; }
        public int TotalDistinctItems { get; set; }

        public List<string> Categories { get; set; } = new();
        public List<string> Departments { get; set; } = new();
        public List<string> Kitchens { get; set; } = new();

        public List<UserSalesItemRowVM> Items { get; set; } = new();
    }

    public class UserSalesItemRowVM
    {
        public string ItemName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }
}