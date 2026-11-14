using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using TimeSeriesApi.Models.Api;
using TimeSeriesApi.Models.Enums;

namespace TimeSeriesApi.Tests.Api
{
    public class TimeSeriesApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly HttpClient _client;

        public TimeSeriesApiTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Full_crud_lifecycle()
        {
            // Create
            var post = await _client.PostAsJsonAsync("/api/timeseries", TestData.ValidFlow(), Json);
            Assert.Equal(HttpStatusCode.Created, post.StatusCode);
            Assert.NotNull(post.Headers.Location);

            var created = await post.Content.ReadFromJsonAsync<TimeSeriesResponse>(Json);
            Assert.Equal(4, created!.Points.Count);
            Assert.Equal(created.Start.AddMinutes(15), created.Points[1].Timestamp);

            // Read
            var get = await _client.GetAsync(post.Headers.Location);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);

            // Update
            var update = TestData.ValidFlow();
            update.Points[0].Quantity = 500m;
            var put = await _client.PutAsJsonAsync($"/api/timeseries/{created.Id}", update, Json);
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            var updated = await put.Content.ReadFromJsonAsync<TimeSeriesResponse>(Json);
            Assert.Equal(500m, updated!.Points[0].Quantity);

            // Delete
            var delete = await _client.DeleteAsync($"/api/timeseries/{created.Id}");
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var afterDelete = await _client.GetAsync($"/api/timeseries/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
        }

        [Fact]
        public async Task Filter_by_zone_and_interval()
        {
            var request = TestData.ValidFlow();
            request.BiddingZone = BiddingZone.NO2;
            request.ToBiddingZone = BiddingZone.DK1;
            await _client.PostAsJsonAsync("/api/timeseries", request, Json);

            var result = await _client.GetFromJsonAsync<List<TimeSeriesResponse>>(
                "/api/timeseries?biddingZone=DK1&from=2026-11-13T00:30:00Z&to=2026-11-13T02:00:00Z", Json);

            Assert.Contains(result!, s => s.BiddingZone == BiddingZone.NO2 && s.ToBiddingZone == BiddingZone.DK1);
        }

        [Fact]
        public async Task Business_rule_violation_returns_400_problem_details()
        {
            var request = TestData.ValidFlow();
            request.ToBiddingZone = request.BiddingZone;

            var response = await _client.PostAsJsonAsync("/api/timeseries", request, Json);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
            Assert.Contains("ToBiddingZone", problem!.Errors.Keys);
        }

        [Fact]
        public async Task Missing_required_field_and_unknown_zone_return_400()
        {
            var missingType = """{ "biddingZone": "DK1", "resolution": "PT60M", "start": "2026-11-13T00:00:00Z", "points": [ { "position": 1, "quantity": 1 } ] }""";
            var unknownZone = """{ "type": "Production", "biddingZone": "DK3", "resolution": "PT60M", "start": "2026-11-13T00:00:00Z", "points": [ { "position": 1, "quantity": 1 } ] }""";

            foreach (var body in new[] { missingType, unknownZone })
            {
                var response = await _client.PostAsync("/api/timeseries", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task Unknown_id_returns_404()
        {
            var response = await _client.GetAsync($"/api/timeseries/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("""{ "type": "Production", "biddingZone": "DK1", "resolution": 99, "start": "2026-11-13T00:00:00Z", "points": [ { "position": 1, "quantity": 1 } ] }""")]
        [InlineData("""{ "type": "Production", "biddingZone": 99, "resolution": "PT60M", "start": "2026-11-13T00:00:00Z", "points": [ { "position": 1, "quantity": 1 } ] }""")]
        [InlineData("""{ "type": 0, "biddingZone": "DK1", "resolution": "PT60M", "start": "2026-11-13T00:00:00Z", "points": [ { "position": 1, "quantity": 1 } ] }""")]
        [InlineData("""{ "type": "Production", "biddingZone": "DK1", "resolution": "1", "start": "2026-11-13T00:00:00Z", "points": [ { "position": 1, "quantity": 1 } ] }""")]
        public async Task Numeric_enum_values_return_400_and_do_not_poison_the_store(string body)
        {
            var response = await PostRaw(body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // Before the fix the invalid series was stored, and every list request returned 500 afterwards.
            var list = await _client.GetAsync("/api/timeseries");
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        }

        private Task<HttpResponseMessage> PostRaw(string body) =>
            _client.PostAsync("/api/timeseries", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
    }
}