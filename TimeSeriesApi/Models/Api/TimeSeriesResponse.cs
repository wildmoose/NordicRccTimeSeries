using System;
using TimeSeriesApi.Models.Enums;

namespace TimeSeriesApi.Models.Api
{
    public class TimeSeriesResponse
    {
        public Guid Id { get; set; }

        public TimeSeriesType Type { get; set; }

        public BiddingZone BiddingZone { get; set; }

        public BiddingZone? ToBiddingZone { get; set; }

        public Resolution Resolution { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset End { get; set; }

        public string Unit { get; set; } = string.Empty;

        public List<TimeSeriesPointResponse> Points { get; set; } = [];

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}