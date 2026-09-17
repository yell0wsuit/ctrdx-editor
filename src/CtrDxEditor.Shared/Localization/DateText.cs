using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CtrDxEditor.Localization
{
    /// <summary>Formats dates for display with the month spelled out, so day and month can't be confused.</summary>
    public static partial class DateText
    {
        /// <summary>
        /// Formats <paramref name="value"/> as the culture's long date without the weekday, then its short
        /// time: "September 17, 2026, 2:32 PM" in en-US, "17 September 2026, 14:32" in en-GB.
        /// </summary>
        /// <param name="value">The moment to format, already in the zone it should be shown in.</param>
        /// <param name="culture">The culture whose date order, month names and clock are used.</param>
        /// <returns>The formatted date and time.</returns>
        public static string LongDateTime(DateTimeOffset value, CultureInfo culture)
        {
            DateTimeFormatInfo format = culture.DateTimeFormat;
            // Dropping the weekday token (and the separator beside it) keeps the culture's own day, month
            // and year order, which a fixed pattern would override.
            string datePattern = WeekdayToken().Replace(format.LongDatePattern, " ").Trim();
            return value.ToString(datePattern, culture) + ", " + value.ToString(format.ShortTimePattern, culture);
        }

        [GeneratedRegex(@"\s*,?\s*dddd\s*,?\s*")]
        private static partial Regex WeekdayToken();
    }
}
