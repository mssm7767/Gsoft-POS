namespace GSoftPosNew.ViewModels
{
    public class StockMovementItemVM
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } // Purchase, Sale, Adjustment
        public string Reference { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public int QuantityChange { get; set; }
        public decimal UnitCost { get; set; }
        //public decimal TotalValue => QuantityChange * UnitCost;
    }
}

