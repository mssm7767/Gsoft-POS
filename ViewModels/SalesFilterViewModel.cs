using System;
using System.Collections.Generic;
using GSoftPosNew.Models;

namespace GSoftPosNew.ViewModels
{
    // 👉 Modal / print ke liye row model: Item + Total Qty
    public class ItemSummaryVM
    {
        public string ItemName { get; set; } = "";
        public decimal TotalQty { get; set; }
    }

    public class SalesFilterViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }
        public int? CustomerId { get; set; }

        // Sales grid ke liye
        public List<Sale> Sales { get; set; } = new List<Sale>();

        // Items Qty modal + print ke liye
        public List<ItemSummaryVM> ItemsSummary { get; set; } = new List<ItemSummaryVM>();
    }
}
