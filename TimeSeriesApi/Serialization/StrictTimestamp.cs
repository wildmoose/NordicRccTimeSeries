using System;
using System.Globalization;

namespace TimeSeriesApi.Serialization
{
    /// <summary>
    /// Parses ISO 8601 timestamps and requires an explicit UTC designator ("Z") or offset ("+01:00").
    /// A timestamp without one is ambiguous, and the built-in parsers silently read it as server-local time.
    /// </summary>
    public static class StrictTimestamp
    {
        public const string ErrorMessage = "Timestamp must be ISO 8601 with 'Z' or an offset, for example 2026-11-13T00:00:00Z or 2026-11-13T01:00:00+01:00.";

        public static bool TryParse(string? text, out DateTimeOffset value)
        {
            value = default;

            if (string.IsNullOrWhiteSpace(text) || !HasExplicitOffset(text))
            {
                return false;
            }

            return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
        }

        private static bool HasExplicitOffset(string text)
        {
            if (text.EndsWith('Z'))
            {
                return true;
            }

            // Ends with +hh:mm or -hh:mm
            return text.Length >= 6
                && (text[^6] == '+' || text[^6] == '-')
                && text[^3] == ':'
                && char.IsAsciiDigit(text[^5]) && char.IsAsciiDigit(text[^4])
                && char.IsAsciiDigit(text[^2]) && char.IsAsciiDigit(text[^1]);
        }
    }
}