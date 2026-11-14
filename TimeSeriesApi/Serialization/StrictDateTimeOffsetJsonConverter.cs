using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimeSeriesApi.Serialization
{
    /// <summary>
    /// Reads DateTimeOffset values from JSON only when they carry "Z" or an offset.
    /// Also used for DateTimeOffset?, because System.Text.Json wraps nullable value types automatically.
    /// </summary>
    public class StrictDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String || !StrictTimestamp.TryParse(reader.GetString(), out var value))
            {
                throw new JsonException(StrictTimestamp.ErrorMessage);
            }

            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}