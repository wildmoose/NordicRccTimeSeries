using System;

namespace TimeSeriesApi.Models.Api
{
    public class TimeSeriesPointResponse
    {
        public int Position { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public decimal Quantity { get; set; }
    }
}