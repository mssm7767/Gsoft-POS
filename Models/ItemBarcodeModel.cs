using System.ComponentModel.DataAnnotations;

namespace GSoftPosNew.Models
{
    public class ItemBarcodeModel
    {
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }

        public string ItemName { get; set; }

        [Required(ErrorMessage = "Barcode is required")]
        public string Barcode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Optional: Navigation Property
        public ItemModel Item { get; set; }
    }

}
