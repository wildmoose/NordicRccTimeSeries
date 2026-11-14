using System;
using TimeSeriesApi.Serialization;

namespace TimeSeriesApi.Tests.Serialization
{
    public class StrictTimestampTests
    {
        [Theory]
        [InlineData("2026-11-13T00:00:00Z", 2026, 11, 13, 0, 0)]
        [InlineData("2026-11-13T01:00:00+01:00", 2026, 11, 13, 0, 0)]
        [InlineData("2026-11-13T05:30:00+05:30", 2026, 11, 13, 0, 0)]
        [InlineData("2026-11-12T19:00:00-05:00", 2026, 11, 13, 0, 0)]
        [InlineData("2026-11-13T00:00:00.000Z", 2026, 11, 13, 0, 0)]
        public void Timestamp_with_offset_is_parsed_to_the_same_instant(string text, int year, int month, int day, int hour, int minute)
        {
            Assert.True(StrictTimestamp.TryParse(text, out var value));
            Assert.Equal(new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero), value);
        }

        [Theory]
        [InlineData("2026-11-13T00:00:00")]
        [InlineData("2026-11-13T00:00")]
        [InlineData("2026-11-13")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("not-a-date")]
        [InlineData("2026-11-13T00:00:00 01:00")] // a "+" in a URL that was not encoded as %2B
        [InlineData("2026-13-45T00:00:00Z")]      // has an offset but is not a real date
        public void Timestamp_without_offset_or_invalid_is_rejected(string? text)
        {
            Assert.False(StrictTimestamp.TryParse(text, out _));
        }
    }
}