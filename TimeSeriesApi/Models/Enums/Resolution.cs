using System;

namespace TimeSeriesApi.Models.Enums
{
    /// <summary>
    /// Interval length between points, named after ISO 8601 durations (as used by ENTSO-E).
    /// </summary>
    public enum Resolution
    {
        PT15M,
        PT60M
    }
}