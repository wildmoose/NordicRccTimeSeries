using System;
using TimeSeriesApi.Models;
using TimeSeriesApi.Models.Api;

namespace TimeSeriesApi.Services
{
    /// <summary>
    /// Maps between the API contract and the domain model.
    /// </summary>
    public static class TimeSeriesMapper
    {
        public static TimeSeries ToDomain(TimeSeriesRequest request)
        {
            return new TimeSeries
            {
                Type = request.Type!.Value,
                BiddingZone = request.BiddingZone!.Value,
                ToBiddingZone = request.ToBiddingZone,
                Resolution = request.Resolution!.Value,
                Start = request.Start!.Value.ToUniversalTime(),
                Unit = request.Unit,
                Points = request.Points
                    .OrderBy(p => p.Position)
                    .Select(p => new TimeSeriesPoint { Position = p.Position, Quantity = p.Quantity })
                    .ToList()
            };
        }

        public static TimeSeriesResponse ToResponse(TimeSeries series)
        {
            return new TimeSeriesResponse
            {
                Id = series.Id,
                Type = series.Type,
                BiddingZone = series.BiddingZone,
                ToBiddingZone = series.ToBiddingZone,
                Resolution = series.Resolution,
                Start = series.Start,
                End = series.End,
                Unit = series.Unit,
                Points = series.Points
                    .Select(p => new TimeSeriesPointResponse
                    {
                        Position = p.Position,
                        Timestamp = series.Start + (series.ResolutionDuration * (p.Position - 1)),
                        Quantity = p.Quantity
                    })
                    .ToList(),
                CreatedAt = series.CreatedAt,
                UpdatedAt = series.UpdatedAt
            };
        }
    }
}