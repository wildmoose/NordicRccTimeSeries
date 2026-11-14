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

            var updated = await _repository.UpdateAsync(changed, CancellationToken.None);
            Assert.NotNull(updated);
            Assert.Equal(999, updated.Points[0].Quantity);

            var stored = await _repository.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.Equal(999, stored!.Points[0].Quantity);
            Assert.Equal(created.CreatedAt, stored.CreatedAt);
            Assert.Equal(created.CreatedAt.AddMinutes(5), stored.UpdatedAt);
        }

        [Fact]
        public async Task Update_of_unknown_id_returns_null_and_delete_returns_false()
        {
            var series = TimeSeriesMapper.ToDomain(TestData.ValidFlow());
            series.Id = Guid.NewGuid();

            Assert.Null(await _repository.UpdateAsync(series, CancellationToken.None));
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

        [Fact]
        public async Task List_order_is_deterministic_when_start_and_zone_are_equal()
        {
            for (var i = 0; i < 20; i++)
            {
                await _repository.AddAsync(TimeSeriesMapper.ToDomain(TestData.ValidFlow()), CancellationToken.None);
            }

            var first = await _repository.GetAsync(new TimeSeriesQuery(), CancellationToken.None);
            var second = await _repository.GetAsync(new TimeSeriesQuery(), CancellationToken.None);

            Assert.Equal(first.Select(s => s.Id), second.Select(s => s.Id));
            Assert.Equal(first.Select(s => s.Id).Order(), first.Select(s => s.Id));
        }

        [Fact]
        public async Task Parallel_update_and_delete_never_resurrects_a_deleted_series()
        {
            for (var i = 0; i < 500; i++)
            {
                var created = await _repository.AddAsync(TimeSeriesMapper.ToDomain(TestData.ValidFlow()), CancellationToken.None);
                var replacement = TimeSeriesMapper.ToDomain(TestData.ValidFlow());
                replacement.Id = created.Id;

                var update = Task.Run(() => _repository.UpdateAsync(replacement, CancellationToken.None));
                var delete = Task.Run(() => _repository.DeleteAsync(created.Id, CancellationToken.None));
                await Task.WhenAll(update, delete);

                // Whatever the interleaving, the delete succeeded and the series is gone afterwards.
                Assert.True(await delete);
                Assert.Null(await _repository.GetByIdAsync(created.Id, CancellationToken.None));
            }
        }
    }
}