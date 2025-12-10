namespace GSoftPosNew.Models
{
    public class SoftwareLicense
    {
        public int Id { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
    }
}
