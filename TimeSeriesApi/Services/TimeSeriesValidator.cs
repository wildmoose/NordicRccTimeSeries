using System;
using TimeSeriesApi.Models.Api;
using TimeSeriesApi.Models.Enums;

namespace TimeSeriesApi.Services
{
    public interface ITimeSeriesValidator
    {
        /// <summary>
        /// Returns validation errors keyed by field name. An empty dictionary means the request is valid.
        /// </summary>
        Dictionary<string, string[]> Validate(TimeSeriesRequest request);

        Dictionary<string, string[]> Validate(TimeSeriesQuery query);
    }

    /// <summary>
    /// Cross-field business rules that DataAnnotations cannot express.
    /// Assumes [Required] checks have already passed (handled by [ApiController]).
    /// </summary>
    public class TimeSeriesValidator : ITimeSeriesValidator
    {
        public Dictionary<string, string[]> Validate(TimeSeriesRequest request)
        {
            var errors = new Dictionary<string, List<string>>();

            ValidateEnums(request, errors);
            ValidateZones(request, errors);
            ValidateStart(request, errors);
            ValidatePoints(request, errors);

            return errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
        }

        public Dictionary<string, string[]> Validate(TimeSeriesQuery query)
        {
            var errors = new Dictionary<string, string[]>();

            if (query.From.HasValue && query.To.HasValue && query.From >= query.To)
            {
                errors[nameof(TimeSeriesQuery.From)] = ["'from' must be earlier than 'to'."];
            }

            return errors;
        }

        /// <summary>
        /// Defence in depth: the JSON converter already rejects numbers and unknown names,
        /// but an undefined enum value must never reach the domain model, for example via code that builds a request directly.
        /// </summary>
        private static void ValidateEnums(TimeSeriesRequest request, Dictionary<string, List<string>> errors)
        {
            if (request.Type is { } type && !Enum.IsDefined(type))
            {
                Add(errors, nameof(request.Type), "Type is not a known value.");
            }

            if (request.BiddingZone is { } zone && !Enum.IsDefined(zone))
            {
                Add(errors, nameof(request.BiddingZone), "BiddingZone is not a known value.");
            }

            if (request.ToBiddingZone is { } toZone && !Enum.IsDefined(toZone))
            {
                Add(errors, nameof(request.ToBiddingZone), "ToBiddingZone is not a known value.");
            }

            if (request.Resolution is { } resolution && !Enum.IsDefined(resolution))
            {
                Add(errors, nameof(request.Resolution), "Resolution is not a known value.");
            }
        }

        private static void ValidateZones(TimeSeriesRequest request, Dictionary<string, List<string>> errors)
        {
            if (request.Type == TimeSeriesType.CrossBorderFlow)
            {
                if (request.ToBiddingZone is null)
                {
                    Add(errors, nameof(request.ToBiddingZone), "ToBiddingZone is required for cross-border flows.");
                }
                else if (request.ToBiddingZone == request.BiddingZone)
                {
                    Add(errors, nameof(request.ToBiddingZone), "ToBiddingZone must differ from BiddingZone.");
                }
            }
            else if (request.ToBiddingZone is not null)
            {
                Add(errors, nameof(request.ToBiddingZone), "ToBiddingZone is only allowed for cross-border flows.");
            }
        }

        private static void ValidateStart(TimeSeriesRequest request, Dictionary<string, List<string>> errors)
        {
            if (request.Start is null || request.Resolution is not { } resolution || !Enum.IsDefined(resolution))
            {
                return;
            }

            var start = request.Start.Value.ToUniversalTime();
            var minutes = resolution == Resolution.PT15M ? 15 : 60;

            if (start.Second != 0 || start.Millisecond != 0 || start.Minute % minutes != 0)
            {
                Add(errors, nameof(request.Start), $"Start must be aligned to the {request.Resolution} resolution.");
            }
        }

        private static void ValidatePoints(TimeSeriesRequest request, Dictionary<string, List<string>> errors)
        {
            if (request.Points.Count == 0)
            {
                return;
            }

            var positions = request.Points.Select(p => p.Position).OrderBy(p => p).ToList();
            var expected = Enumerable.Range(1, positions.Count);

            if (!positions.SequenceEqual(expected))
            {
                Add(errors, nameof(request.Points), "Point positions must be unique and run from 1 to n without gaps.");
            }
        }

        private static void Add(Dictionary<string, List<string>> errors, string key, string message)
        {
            if (!errors.TryGetValue(key, out var list))
            {
                list = [];
                errors[key] = list;
            }

            list.Add(message);
        }
    }
}