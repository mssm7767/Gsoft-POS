using System.Drawing.Printing;

namespace GSoftPosNew.Models
{
    public class StockAdjustment
    {
        public int Id { get; set; }
        public string? RefNo { get; set; }
        public string? Note { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        public IEnumerable<StockAdjustmentItem> StockAdjustmentItems { get; set; }
    }
}
