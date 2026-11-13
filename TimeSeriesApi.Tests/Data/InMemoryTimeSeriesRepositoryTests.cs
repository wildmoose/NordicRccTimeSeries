using System;
using Microsoft.Extensions.Time.Testing;
using TimeSeriesApi.Data;
using TimeSeriesApi.Models.Api;
using TimeSeriesApi.Models.Enums;
using TimeSeriesApi.Services;

namespace TimeSeriesApi.Tests.Data
{
    public class InMemoryTimeSeriesRepositoryTests
    {
        private static readonly DateTimeOffset Midnight = new(2026, 11, 13, 0, 0, 0, TimeSpan.Zero);

        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero));
        private readonly InMemoryTimeSeriesRepository _repository;

        public InMemoryTimeSeriesRepositoryTests()
        {
            _repository = new InMemoryTimeSeriesRepository(_time);
        }

        [Fact]
        public async Task Add_assigns_id_and_timestamps()
        {
            var created = await _repository.AddAsync(TimeSeriesMapper.ToDomain(TestData.ValidFlow()), CancellationToken.None);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal(_time.GetUtcNow(), created.CreatedAt);
            Assert.Equal(Midnight.AddHours(1), created.End); // 4 x 15 min
        }

        [Fact]
        public async Task Flow_is_found_by_both_sending_and_receiving_zone()
        {
            await _repository.AddAsync(TimeSeriesMapper.ToDomain(TestData.ValidFlow()), CancellationToken.None);

            var bySender = await _repository.GetAsync(new TimeSeriesQuery { BiddingZone = BiddingZone.DK2 }, CancellationToken.None);
            var byReceiver = await _repository.GetAsync(new TimeSeriesQuery { BiddingZone = BiddingZone.SE4 }, CancellationToken.None);
            var other = await _repository.GetAsync(new TimeSeriesQuery { BiddingZone = BiddingZone.NO1 }, CancellationToken.None);

            Assert.Single(bySender);
            Assert.Single(byReceiver);
            Assert.Empty(other);
        }

        [Theory]
        [InlineData(0, 1, true)]     // exact match
        [InlineData(-1, 0.5, true)]  // overlaps start
        [InlineData(0.5, 2, true)]   // overlaps end
        [InlineData(1, 2, false)]    // starts exactly at End (half-open, no overlap)
        [InlineData(-2, 0, false)]   // ends exactly at Start
        public async Task Interval_filter_uses_overlap(double fromHours, double toHours, bool expected)
        {
            await _repository.AddAsync(TimeSeriesMapper.ToDomain(TestData.ValidFlow()), CancellationToken.None);

            var query = new TimeSeriesQuery { From = Midnight.AddHours(fromHours), To = Midnight.AddHours(toHours) };
            var result = await _repository.GetAsync(query, CancellationToken.None);

            Assert.Equal(expected, result.Count == 1);
        }

        [Fact]
        public async Task Update_keeps_created_and_changes_updated()
        {
            var created = await _repository.AddAsync(TimeSeriesMapper.ToDomain(TestData.ValidFlow()), CancellationToken.None);
            _time.Advance(TimeSpan.FromMinutes(5));

            var changed = TimeSeriesMapper.ToDomain(TestData.ValidFlow());
            changed.Id = created.Id;
            changed.Points[0].Quantity = 999;

            Assert.True(await _repository.UpdateAsync(changed, CancellationToken.None));

            var stored = await _repository.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.Equal(999, stored!.Points[0].Quantity);
            Assert.Equal(created.CreatedAt, stored.CreatedAt);
            Assert.Equal(created.CreatedAt.AddMinutes(5), stored.UpdatedAt);
        }

        [Fact]
        public async Task Update_and_delete_of_unknown_id_return_false()
        {
            var series = TimeSeriesMapper.ToDomain(TestData.ValidFlow());
            series.Id = Guid.NewGuid();

            Assert.False(await _repository.UpdateAsync(series, CancellationToken.None));
            Assert.False(await _repository.DeleteAsync(series.Id, CancellationToken.None));
        }

        [Fact]
        public async Task Returned_objects_are_copies()
        {
            var created = await _repository.AddAsync(TimeSeriesMapper.ToDomain(TestData.ValidFlow()), CancellationToken.None);
            created.Points.Clear();

            var stored = await _repository.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.Equal(4, stored!.Points.Count);
        }
    }
}