namespace GSoftPosNew.Models
{
    public class SaveMultiBarcodesDto
    {
        public int ItemId { get; set; }
        public List<string> MultiBarcodes { get; set; } = new();
    }

}
