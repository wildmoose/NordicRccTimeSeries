using System;
using TimeSeriesApi.Models;
using TimeSeriesApi.Models.Api;

namespace TimeSeriesApi.Data
{
    public interface ITimeSeriesRepository
    {
        Task<IReadOnlyList<TimeSeries>> GetAsync(TimeSeriesQuery query, CancellationToken ct);

        Task<TimeSeries?> GetByIdAsync(Guid id, CancellationToken ct);

        Task<TimeSeries> AddAsync(TimeSeries series, CancellationToken ct);

        /// <summary>Returns the updated series, or null if the series does not exist.</summary>
        Task<TimeSeries?> UpdateAsync(TimeSeries series, CancellationToken ct);

        /// <summary>Returns false if the series does not exist.</summary>
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}