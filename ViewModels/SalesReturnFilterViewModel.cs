using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GSoftPosNew.ViewModels
{
    // Grid ke ek row ka data
    public class SalesReturnRow
    {
        public string InvoiceNo { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Amount { get; set; }
        public DateTime ReturnDate { get; set; }

        public string CategoryName { get; set; }
        public string SupplierName { get; set; }
    }

    // Puri page ka filter + rows
    public class SalesReturnFilterViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string? ItemSearch { get; set; }

        public int CategoryId { get; set; }
        public int SupplierId { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Suppliers { get; set; } = new();

        public List<SalesReturnRow> Rows { get; set; } = new();
    }
}
