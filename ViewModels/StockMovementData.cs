namespace GSoftPosNew.ViewModels
{
    public class StockMovementData
    {
        public DateTime Date { get; set; }
        public string Reference { get; set; }
        public string Type { get; set; } // Purchase, Sale, Adjustment
        public string ItemName { get; set; }
        public int QuantityChange { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue => QuantityChange * UnitCost;
    }
}
