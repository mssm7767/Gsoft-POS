using System.ComponentModel.DataAnnotations;

namespace GSoftPosNew.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category Name is required")]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(250)]
        public string? Description { get; set; }

        public decimal Discount { get; set; } = 0;

        public string? ImagePath { get; set; }

        public ICollection<ItemModel> Items { get; set; } = new List<ItemModel>();
    }
}

