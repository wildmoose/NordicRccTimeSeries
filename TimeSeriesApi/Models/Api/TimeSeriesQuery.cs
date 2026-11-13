using System;
using TimeSeriesApi.Models.Enums;

namespace TimeSeriesApi.Models.Api
{
    /// <summary>
    /// Query parameters for GET /api/timeseries.
    /// </summary>
    public class TimeSeriesQuery
    {
        public BiddingZone? BiddingZone { get; set; }

        public TimeSeriesType? Type { get; set; }

        /// <summary>Inclusive lower bound of the interval.</summary>
        public DateTimeOffset? From { get; set; }

        /// <summary>Exclusive upper bound of the interval.</summary>
        public DateTimeOffset? To { get; set; }
    }
}