using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GSoftPosNew.Models
{
    public class MultiBarcodes
    {
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }

        [Required]
        [MaxLength(80)]
        public string Barcode { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ItemId))]
        public ItemModel Item { get; set; } = null!;
    }
}
