using GSoftPosNew.Models;

namespace GSoftPosNew.ViewModels
{
    public class SalesFilterViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Search { get; set; }
        public IEnumerable<Sale> Sales { get; set; }
    }

}
