namespace ILOps_Inventory.Common.UtcConverter
{
    public class UtcHelper
    {
        public static DateTime ParseIsoToLocal(string iso)
        {
            return DateTime.Parse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind)
                           .ToLocalTime();
        }

        public static string LocalToIsoUtc(DateTime local)
        {
            return local.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
        }
    }
    }
