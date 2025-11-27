using System.ComponentModel.DataAnnotations;

namespace GSoftPosNew.Models
{
    public class Location
    {
        public int Id { get; set; }

        [Required, Display(Name = "Location Name")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        public List<PosTable> Tables { get; set; }
    }
}
