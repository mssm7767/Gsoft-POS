namespace GSoftPosNew.Models
{
    public class RolePermission
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string ModuleName { get; set; }
        public bool IsAllowed { get; set; }
    }
}
