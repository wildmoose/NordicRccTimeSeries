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
    }
}