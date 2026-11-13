using System;
using System.ComponentModel.DataAnnotations;
using TimeSeriesApi.Models.Enums;

namespace TimeSeriesApi.Models.Api
{
    /// <summary>
    /// Body for POST (create) and PUT (full replace) of a time series.
    /// </summary>
    public class TimeSeriesRequest
    {
        [Required]
        public TimeSeriesType? Type { get; set; }

        [Required]
        public BiddingZone? BiddingZone { get; set; }

        public BiddingZone? ToBiddingZone { get; set; }

        [Required]
        public Resolution? Resolution { get; set; }

        [Required]
        public DateTimeOffset? Start { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 1)]
        public string Unit { get; set; } = "MW";

        [Required]
        [MinLength(1)]
        public List<TimeSeriesPointRequest> Points { get; set; } = [];
    }
}