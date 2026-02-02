using System.Globalization;

namespace ILOps_Inventory.Common.Spire
{
    public class DateHelpers
    {


        public static DateTime UtcStringToDate(string utc)
        {
            if (string.IsNullOrWhiteSpace(utc))
                throw new ArgumentException("UTC string is null or empty.", nameof(utc));

            return DateTime.ParseExact(
                utc,
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }
    }
}
