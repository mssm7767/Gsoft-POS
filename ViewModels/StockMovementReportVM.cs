namespace GSoftPosNew.ViewModels
{
    public class StockMovementReportVM
    {
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.Today;
        public List<StockMovementItemVM> Movements { get; set; } = new List<StockMovementItemVM>();
        public List<StockMovementData> StockMovements { get; set; } = new List<StockMovementData>();
    }
}
