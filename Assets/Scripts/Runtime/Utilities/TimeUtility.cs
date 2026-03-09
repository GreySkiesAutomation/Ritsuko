using System;
using System.Globalization;
namespace Runtime.Utilities
{
    public class TimeUtility
    {
        public const string TimeZoneId = "Central Standard Time";
        
        public static string GetCurrentLocalAustinTimeDisplay()
        {
            var utcNow = DateTime.UtcNow;
            var localTime = ConvertUtcToConfiguredLocalTime(utcNow);

            return localTime.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.InvariantCulture);
        }

        public static DateTime ConvertUtcToConfiguredLocalTime(DateTime utcDateTime)
        {
            try
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZoneInfo);
            }
            catch (TimeZoneNotFoundException)
            {
                return utcDateTime.ToLocalTime();
            }
            catch (InvalidTimeZoneException)
            {
                return utcDateTime.ToLocalTime();
            }
        }
    }
}