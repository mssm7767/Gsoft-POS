using System.Text.Json.Serialization;

namespace GSoftPosNew.Models
{
    public class StockAdjustmentItem
    {
        public int Id { get; set; }
        public int StockAdjustmentId { get; set; }

        [JsonIgnore]
        public StockAdjustment StockAdjustment { get; set; }
        public string ItemCode { get; set; }
        public string? ItemName { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? SalePrice { get; set; }
        public int? ComputerStock { get; set; }
        public int? PhysicalStock { get; set; }
        public int? Difference { get; set; }
        public decimal? TotalDifference { get; set; }
    }
}
