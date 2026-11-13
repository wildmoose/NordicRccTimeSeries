using System;

namespace TimeSeriesApi.Models
{
    /// <summary>
    /// A single value in a time series. Position is 1-based; the point covers
    /// the interval [Start + (Position - 1) * Resolution, Start + Position * Resolution).
    /// </summary>
    public class TimeSeriesPoint
    {
        public int Position { get; set; }

        public decimal Quantity { get; set; }
    }
}