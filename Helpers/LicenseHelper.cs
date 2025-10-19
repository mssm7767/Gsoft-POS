namespace GSoftPosNew.Helpers
{
    public static class LicenseHelper
    {
        public static bool IsExpired(DateTime expiryDate)
        {
            return DateTime.Now.Date > expiryDate.Date;
        }
    }

}
