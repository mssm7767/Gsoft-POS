using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GSoftPosNew.Models
{
    [Table("Units")] // Table name database میں
    public class Unit
    {
        [Key]
        [Column("UnitId")] // DB column UnitId
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Column("UnitName")] // DB column UnitName
        public string Name { get; set; } = string.Empty; // e.g., kg, pcs, pack, meter, gram

        [StringLength(250)]
        [Column("Description")] // DB column Description
        public string? Detail { get; set; }  // optional description
    }
}
