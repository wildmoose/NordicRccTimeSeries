using System;
using TimeSeriesApi.Models.Api;
using TimeSeriesApi.Models.Enums;
using TimeSeriesApi.Services;

namespace TimeSeriesApi.Tests.Services
{
    public class TimeSeriesValidatorTests
    {
        private readonly TimeSeriesValidator _validator = new();

        [Fact]
        public void Valid_request_has_no_errors()
        {
            Assert.Empty(_validator.Validate(TestData.ValidFlow()));
        }

        [Fact]
        public void Flow_without_receiving_zone_is_rejected()
        {
            var request = TestData.ValidFlow();
            request.ToBiddingZone = null;

            Assert.Contains(nameof(TimeSeriesRequest.ToBiddingZone), _validator.Validate(request).Keys);
        }

        [Fact]
        public void Flow_to_same_zone_is_rejected()
        {
            var request = TestData.ValidFlow();
            request.ToBiddingZone = BiddingZone.DK2;

            Assert.Contains(nameof(TimeSeriesRequest.ToBiddingZone), _validator.Validate(request).Keys);
        }

        [Fact]
        public void Receiving_zone_on_production_is_rejected()
        {
            var request = TestData.ValidFlow();
            request.Type = TimeSeriesType.Production;

            Assert.Contains(nameof(TimeSeriesRequest.ToBiddingZone), _validator.Validate(request).Keys);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(20)]
        public void Start_not_aligned_to_resolution_is_rejected(int minute)
        {
            var request = TestData.ValidFlow();
            request.Start = new DateTimeOffset(2026, 11, 13, 10, minute, 0, TimeSpan.Zero);

            Assert.Contains(nameof(TimeSeriesRequest.Start), _validator.Validate(request).Keys);
        }

        [Theory]
        [InlineData(new[] { 1, 2, 4 })]    // gap
        [InlineData(new[] { 1, 1, 2 })]    // duplicate
        [InlineData(new[] { 0, 1, 2 })]    // not 1-based
        public void Invalid_positions_are_rejected(int[] positions)
        {
            var request = TestData.ValidFlow();
            request.Points = positions.Select(p => new TimeSeriesPointRequest { Position = p, Quantity = 1 }).ToList();

            Assert.Contains(nameof(TimeSeriesRequest.Points), _validator.Validate(request).Keys);
        }

        [Fact]
        public void Positions_in_any_order_are_accepted()
        {
            var request = TestData.ValidFlow();
            request.Points.Reverse();

            Assert.Empty(_validator.Validate(request));
        }

        [Fact]
        public void Query_with_from_after_to_is_rejected()
        {
            var query = new TimeSeriesQuery
            {
                From = new DateTimeOffset(2026, 11, 13, 2, 0, 0, TimeSpan.Zero),
                To = new DateTimeOffset(2026, 11, 13, 1, 0, 0, TimeSpan.Zero)
            };

            Assert.NotEmpty(_validator.Validate(query));
        }

        [Fact]
        public void Undefined_resolution_is_rejected()
        {
            var request = TestData.ValidFlow();
            request.Resolution = (Resolution)99;

            Assert.Contains(nameof(TimeSeriesRequest.Resolution), _validator.Validate(request).Keys);
        }

        [Fact]
        public void Undefined_zones_and_type_are_rejected()
        {
            var request = TestData.ValidFlow();
            request.Type = (TimeSeriesType)99;
            request.BiddingZone = (BiddingZone)99;
            request.ToBiddingZone = (BiddingZone)98;

            var errors = _validator.Validate(request);

            Assert.Contains(nameof(TimeSeriesRequest.Type), errors.Keys);
            Assert.Contains(nameof(TimeSeriesRequest.BiddingZone), errors.Keys);
            Assert.Contains(nameof(TimeSeriesRequest.ToBiddingZone), errors.Keys);
        }

        [Fact]
        public void Null_point_is_rejected_instead_of_throwing()
        {
            var request = TestData.ValidFlow();
            request.Points.Add(null!);

            Assert.Contains(nameof(TimeSeriesRequest.Points), _validator.Validate(request).Keys);
        }

        [Fact]
        public void Point_without_quantity_is_rejected()
        {
            var request = TestData.ValidFlow();
            request.Points[1].Quantity = null;

            Assert.Contains(nameof(TimeSeriesRequest.Points), _validator.Validate(request).Keys);
        }

        [Fact]
        public void Point_without_position_is_rejected()
        {
            var request = TestData.ValidFlow();
            request.Points[1].Position = null;

            Assert.Contains(nameof(TimeSeriesRequest.Points), _validator.Validate(request).Keys);
        }

        [Fact]
        public void Negative_quantity_is_rejected()
        {
            var request = TestData.ValidFlow();
            request.Points[2].Quantity = -0.01m;

            Assert.Contains(nameof(TimeSeriesRequest.Points), _validator.Validate(request).Keys);
        }

        [Fact]
        public void Zero_quantity_is_accepted()
        {
            var request = TestData.ValidFlow();
            request.Points[2].Quantity = 0m;

            Assert.Empty(_validator.Validate(request));
        }

        [Theory]
        [InlineData(0, 1, 0)]    // 1 second
        [InlineData(0, 0, 500)]  // 500 milliseconds
        public void Start_with_seconds_or_milliseconds_is_rejected(int minute, int second, int millisecond)
        {
            var request = TestData.ValidFlow();
            request.Start = new DateTimeOffset(2026, 11, 13, 10, minute, second, millisecond, TimeSpan.Zero);

            Assert.Contains(nameof(TimeSeriesRequest.Start), _validator.Validate(request).Keys);
        }

        [Theory]
        [InlineData(Resolution.PT60M, 0, true)]
        [InlineData(Resolution.PT60M, 30, false)]   // aligned to 15 minutes, but not to an hour
        [InlineData(Resolution.PT60M, 15, false)]
        [InlineData(Resolution.PT15M, 15, true)]
        [InlineData(Resolution.PT15M, 45, true)]
        public void Start_alignment_depends_on_resolution(Resolution resolution, int minute, bool valid)
        {
            var request = TestData.ValidFlow();
            request.Resolution = resolution;
            request.Start = new DateTimeOffset(2026, 11, 13, 10, minute, 0, TimeSpan.Zero);

            Assert.Equal(valid, !_validator.Validate(request).ContainsKey(nameof(TimeSeriesRequest.Start)));
        }

        [Theory]
        [InlineData(60, 1, 0, true)]     // 01:00+01:00 = 00:00Z
        [InlineData(60, 1, 30, false)]   // 01:30+01:00 = 00:30Z
        [InlineData(330, 5, 30, true)]   // 05:30+05:30 = 00:00Z: alignment is checked in UTC, not on the local wall clock
        [InlineData(330, 5, 0, false)]   // 05:00+05:30 = 23:30Z the day before
        public void Start_alignment_is_checked_in_utc_for_hourly_series(int offsetMinutes, int hour, int minute, bool valid)
        {
            var request = TestData.ValidFlow();
            request.Resolution = Resolution.PT60M;
            request.Start = new DateTimeOffset(2026, 11, 13, hour, minute, 0, TimeSpan.FromMinutes(offsetMinutes));

            Assert.Equal(valid, !_validator.Validate(request).ContainsKey(nameof(TimeSeriesRequest.Start)));
        }
    }
}