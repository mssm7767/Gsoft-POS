namespace GSoftPosNew.ViewModels
{
    public class ProfitLossItemVM
    {
        public string Name { get; set; }
        public decimal Sales { get; set; } = 0;
        public decimal Cost { get; set; }
        public decimal Profit => Sales - Cost;
        public decimal Margin => Sales > 0 ? Profit / Sales * 100 : 0;
    }
}
