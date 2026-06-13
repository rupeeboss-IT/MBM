using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RB_Website_API.Auth;

/// <summary>
/// Serializes DateTime as local clock time without a trailing Z so the Angular client displays the same wall-clock time.
/// </summary>
public sealed class LocalDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s)) return default;

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            return DateTime.SpecifyKind(AppDateTime.Normalize(parsed), DateTimeKind.Unspecified);

        return default;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var local = AppDateTime.Normalize(value);
        writer.WriteStringValue(local.ToString(Format, CultureInfo.InvariantCulture));
    }
}

public sealed class NullableLocalDateTimeJsonConverter : JsonConverter<DateTime?>
{
    private readonly LocalDateTimeJsonConverter _inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        return _inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        _inner.Write(writer, value.Value, options);
    }
}
