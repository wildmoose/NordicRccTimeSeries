using System;
using System.Collections.Concurrent;
using TimeSeriesApi.Models;
using TimeSeriesApi.Models.Api;

namespace TimeSeriesApi.Data
{
    /// <summary>
    /// Thread-safe in-memory store. Data is lost when the process stops.
    /// Stores and returns copies so callers cannot mutate stored state by accident.
    /// </summary>
    public class InMemoryTimeSeriesRepository : ITimeSeriesRepository
    {
        private readonly ConcurrentDictionary<Guid, TimeSeries> _store = new();
        private readonly TimeProvider _timeProvider;

        public InMemoryTimeSeriesRepository(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public Task<IReadOnlyList<TimeSeries>> GetAsync(TimeSeriesQuery query, CancellationToken ct)
        {
            IEnumerable<TimeSeries> result = _store.Values;

            if (query.BiddingZone.HasValue)
            {
                // A cross-border flow belongs to both the sending and the receiving zone.
                result = result.Where(s => s.BiddingZone == query.BiddingZone || s.ToBiddingZone == query.BiddingZone);
            }

            if (query.Type.HasValue)
            {
                result = result.Where(s => s.Type == query.Type);
            }

            // Overlap test on half-open intervals: [Start, End) overlaps [From, To)
            if (query.From.HasValue)
            {
                result = result.Where(s => s.End > query.From.Value);
            }

            if (query.To.HasValue)
            {
                result = result.Where(s => s.Start < query.To.Value);
            }

            IReadOnlyList<TimeSeries> list = result
                .OrderBy(s => s.Start)
                .ThenBy(s => s.BiddingZone)
                .Select(Copy)
                .ToList();

            return Task.FromResult(list);
        }

        public Task<TimeSeries?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var found = _store.TryGetValue(id, out var series) ? Copy(series) : null;
            return Task.FromResult(found);
        }

        public Task<TimeSeries> AddAsync(TimeSeries series, CancellationToken ct)
        {
            var now = _timeProvider.GetUtcNow();
            var stored = Copy(series);
            stored.Id = Guid.NewGuid();
            stored.CreatedAt = now;
            stored.UpdatedAt = now;

            _store[stored.Id] = stored;
            return Task.FromResult(Copy(stored));
        }

        public Task<bool> UpdateAsync(TimeSeries series, CancellationToken ct)
        {
            while (_store.TryGetValue(series.Id, out var existing))
            {
                var updated = Copy(series);
                updated.CreatedAt = existing.CreatedAt;
                updated.UpdatedAt = _timeProvider.GetUtcNow();

                // Only replace if nobody else changed or deleted it in the meantime.
                if (_store.TryUpdate(series.Id, updated, existing))
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            return Task.FromResult(_store.TryRemove(id, out _));
        }

        private static TimeSeries Copy(TimeSeries source)
        {
            return new TimeSeries
            {
                Id = source.Id,
                Type = source.Type,
                BiddingZone = source.BiddingZone,
                ToBiddingZone = source.ToBiddingZone,
                Resolution = source.Resolution,
                Start = source.Start,
                Unit = source.Unit,
                Points = source.Points
                    .Select(p => new TimeSeriesPoint { Position = p.Position, Quantity = p.Quantity })
                    .ToList(),
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
        }
    }
}