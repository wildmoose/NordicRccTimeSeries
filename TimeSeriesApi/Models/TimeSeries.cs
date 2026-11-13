using System;

using TimeSeriesApi.Models.Enums;

namespace TimeSeriesApi.Models
{
    /// <summary>
    /// A time series of electricity market values for one bidding zone (or a flow between two zones).
    /// </summary>
    public class TimeSeries
    {
        public Guid Id { get; set; }

        public TimeSeriesType Type { get; set; }

        /// <summary>
        /// The bidding zone the series belongs to. For cross-border flows this is the "from" zone.
        /// </summary>
        public BiddingZone BiddingZone { get; set; }

        /// <summary>
        /// Only used for cross-border flows: the receiving bidding zone.
        /// </summary>
        public BiddingZone? ToBiddingZone { get; set; }

        public Resolution Resolution { get; set; }

        /// <summary>
        /// Start of the period in UTC.
        /// </summary>
        public DateTimeOffset Start { get; set; }

        /// <summary>
        /// Unit of measure, e.g. "MW".
        /// </summary>
        public string Unit { get; set; } = "MW";

        public List<TimeSeriesPoint> Points { get; set; } = [];

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// End of the period (exclusive), derived from Start, Resolution and number of points.
        /// </summary>
        public DateTimeOffset End => Start + (ResolutionDuration * Points.Count);

        public TimeSpan ResolutionDuration => Resolution switch
        {
            Resolution.PT15M => TimeSpan.FromMinutes(15),
            Resolution.PT60M => TimeSpan.FromMinutes(60),
            _ => throw new InvalidOperationException($"Unsupported resolution {Resolution}.")
        };
    }
}