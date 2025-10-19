namespace GSoftPosNew.ViewModels
{
    public class InvoiceSlipViewModel
    {
        public string LogoUrl { get; set; }
        public string ShopName { get; set; }
        public string ShopAddress { get; set; }
        public string ShopContact { get; set; }
        public string CustomerName { get; set; }
        public string InvoiceNumber { get; set; }
        public string CashierName { get; set; }
        public DateTime InvoiceDateTime { get; set; }
        // اگر آئٹمز بھی پرنٹ کرنا ہیں تو یہاں ایک List<InvoiceItem> رکھیں
    }
}
