using Microsoft.AspNetCore.Mvc.Rendering;

namespace GSoftPosNew.ViewModels
{
    public class ItemLedgerReportVM
    {
        public int? ItemId { get; set; }
        public string ItemSearch { get; set; } = "";

        public string ItemName { get; set; } = "";
        public string ItemCode { get; set; } = "";

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public decimal TotalPurchaseQty { get; set; }
        public decimal TotalPurchaseReturnQty { get; set; }
        public decimal TotalSaleQty { get; set; }
        public decimal TotalSaleReturnQty { get; set; }

        public decimal NetPurchaseQty { get; set; }
        public decimal NetSaleQty { get; set; }

        public decimal TotalPurchaseAmount { get; set; }
        public decimal TotalPurchaseReturnAmount { get; set; }
        public decimal TotalSaleAmount { get; set; }
        public decimal TotalSaleReturnAmount { get; set; }

        public decimal GrossProfit { get; set; }
        public decimal ReturnLoss { get; set; }
        public decimal NetProfit { get; set; }

        public List<ItemLedgerRowVM> Rows { get; set; } = new List<ItemLedgerRowVM>();

        public List<SelectListItem> Items { get; set; } = new List<SelectListItem>();
    }

    public class ItemLedgerRowVM
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = "";
        public string InvoiceNo { get; set; } = "";
        public decimal QtyIn { get; set; }
        public decimal QtyOut { get; set; }
        public decimal PurchaseRate { get; set; }
        public decimal SaleRate { get; set; }
        public decimal Amount { get; set; }
        public decimal Profit { get; set; }
    }
}