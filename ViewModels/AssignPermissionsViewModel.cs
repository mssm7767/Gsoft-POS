using Microsoft.AspNetCore.Mvc.Rendering;

namespace GSoftPosNew.ViewModels
{
    public class AssignPermissionsViewModel
    {
        public int RoleId { get; set; }
        public List<SelectListItem> RoleList { get; set; }
        public List<string> Modules { get; set; }
        public List<string> SelectedModules { get; set; }
    }
}
