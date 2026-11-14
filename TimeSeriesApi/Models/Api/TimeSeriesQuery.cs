using System;
using Microsoft.AspNetCore.Mvc;
using TimeSeriesApi.Models.Enums;
using TimeSeriesApi.Serialization;

namespace TimeSeriesApi.Models.Api
{
    /// <summary>
    /// Query parameters for GET /api/timeseries.
    /// </summary>
    public class TimeSeriesQuery
    {
        public BiddingZone? BiddingZone { get; set; }

        public TimeSeriesType? Type { get; set; }

        /// <summary>Inclusive lower bound of the interval. Must include "Z" or an offset (encode "+" as %2B in a URL).</summary>
        [ModelBinder(typeof(StrictDateTimeOffsetModelBinder))]
        public DateTimeOffset? From { get; set; }

        /// <summary>Exclusive upper bound of the interval. Must include "Z" or an offset (encode "+" as %2B in a URL).</summary>
        [ModelBinder(typeof(StrictDateTimeOffsetModelBinder))]
        public DateTimeOffset? To { get; set; }
    }
}