namespace GSoftPosNew.Models
{
    public class PosTable
    {
        public int Id { get; set; }
        public int? LocationId { get; set; }
        public string? TableName { get; set; }
        public string? TableCode { get; set; }
        public int? Capacity { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public string? Status { get; set; } = "free";
        public Location Location { get; set; }
    }
}
