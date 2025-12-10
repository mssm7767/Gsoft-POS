using GSoftPosNew.Data;

namespace GSoftPosNew.Services
{
    public class LicenseService
    {
        private readonly AppDbContext _db;

        public LicenseService(AppDbContext db)
        {
            _db = db;
        }

        public DateTime GetExpiryDate()
        {
            return _db.SoftwareLicense.First().ExpiryDate;
        }

        public bool IsExpired()
        {
            DateTime expiry = GetExpiryDate();
            return DateTime.Today > expiry;
        }
    }
}
