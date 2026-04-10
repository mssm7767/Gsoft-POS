namespace GSoftPosNew.Models
{
    public class TerminalPrinterSetting
    {
        public int Id { get; set; }

        public string StationName { get; set; } = "";

        public string PCName { get; set; } = "";

        public string ReceiptPrinterPath { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }
}