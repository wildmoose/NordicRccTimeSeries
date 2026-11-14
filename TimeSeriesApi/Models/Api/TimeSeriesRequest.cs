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
        /// <summary>One leap year of 15-minute points (366 * 96). A sanity limit, not a business rule.</summary>
        public const int MaxPoints = 35_136;

        [Required]
        public TimeSeriesType? Type { get; set; }

        [Required]
        public BiddingZone? BiddingZone { get; set; }

        public BiddingZone? ToBiddingZone { get; set; }

        [Required]
        public Resolution? Resolution { get; set; }

        [Required]
        public DateTimeOffset? Start { get; set; }

        /// <summary>Unit of the quantities: "MW" (power) or "MWh" (energy). Values are stored as given, not converted.</summary>
        [Required]
        [AllowedValues("MW", "MWh")]
        public string Unit { get; set; } = "MW";

        [Required]
        [MinLength(1)]
        [MaxLength(MaxPoints)]
        public List<TimeSeriesPointRequest> Points { get; set; } = [];
    }
}