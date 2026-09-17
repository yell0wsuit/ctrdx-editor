using System;
using System.Globalization;

using CtrDxEditor.Localization;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the spelled-out date and time shown to users.</summary>
    public class DateTextTests
    {
        private static readonly DateTimeOffset Moment = new(2026, 9, 17, 14, 32, 0, TimeSpan.Zero);

        /// <summary>The month is spelled out in the culture's own order, with no weekday.</summary>
        [Theory]
        [InlineData("en-US", "September 17, 2026")]
        [InlineData("en-GB", "17 September 2026")]
        [InlineData("de-DE", "17. September 2026")]
        public void SpellsMonthInCultureOrder(string cultureName, string expectedDate)
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);

            string text = DateText.LongDateTime(Moment, culture);

            Assert.StartsWith(expectedDate + ", ", text, StringComparison.Ordinal);
            Assert.EndsWith(Moment.ToString(culture.DateTimeFormat.ShortTimePattern, culture), text, StringComparison.Ordinal);
            Assert.DoesNotContain(culture.DateTimeFormat.GetDayName(Moment.DayOfWeek), text, StringComparison.Ordinal);
        }
    }
}
