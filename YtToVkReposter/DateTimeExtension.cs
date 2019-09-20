using System;
using System.Globalization;

namespace YtToVkReposter
{
    public static class DateTimeExtension
    {
        public static string ToRfc3339String(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", DateTimeFormatInfo.InvariantInfo);
        }
        
        public static DateTime ToRfc3339S(this DateTime dateTime)
        {
            return DateTime.Parse(dateTime.ToRfc3339String(), DateTimeFormatInfo.InvariantInfo);
        }
        
        public static string ToISO8601String(this DateTime dateTime)
        {
            return dateTime.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
        }
        
        public static DateTime ToISO8601S(this DateTime dateTime)
        {
            return DateTime.Parse(dateTime.ToISO8601String());
        }
        
    }
}