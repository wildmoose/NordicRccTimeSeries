using System;

namespace TimeSeriesApi.Models.Api
{
    public class TimeSeriesPointRequest
    {
        public int Position { get; set; }
        public decimal Quantity { get; set; }
    }
}
