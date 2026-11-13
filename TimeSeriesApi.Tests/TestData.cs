using System;
using TimeSeriesApi.Models.Api;
using TimeSeriesApi.Models.Enums;

namespace TimeSeriesApi.Tests
{
    public static class TestData
    {
        public static TimeSeriesRequest ValidFlow()
        {
            return new TimeSeriesRequest
            {
                Type = TimeSeriesType.CrossBorderFlow,
                BiddingZone = BiddingZone.DK2,
                ToBiddingZone = BiddingZone.SE4,
                Resolution = Resolution.PT15M,
                Start = new DateTimeOffset(2026, 11, 13, 0, 0, 0, TimeSpan.Zero),
                Unit = "MW",
                Points =
                [
                    new TimeSeriesPointRequest { Position = 1, Quantity = 412.5m },
                    new TimeSeriesPointRequest { Position = 2, Quantity = 398.0m },
                    new TimeSeriesPointRequest { Position = 3, Quantity = 405.25m },
                    new TimeSeriesPointRequest { Position = 4, Quantity = 420.0m }
                ]
            };
        }
    }
}