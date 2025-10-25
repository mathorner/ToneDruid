using System.Text.Json;
using System.Text.Json.Serialization;

namespace ToneDruid.Api.Models.VoiceParameters;

internal sealed class VoiceParameterEnumValueConverter : JsonConverter<VoiceParameterEnumValue>
{
    public override VoiceParameterEnumValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException("Enum value cannot be null or whitespace.");
            }

            return new VoiceParameterEnumValue
            {
                Value = value,
                Label = null
            };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            if (!root.TryGetProperty("value", out var valueElement))
            {
                throw new JsonException("Enum value object must contain a 'value' property.");
            }

            var value = valueElement.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException("Enum value object must provide a non-empty 'value'.");
            }

            string? label = null;
            if (root.TryGetProperty("label", out var labelElement) && labelElement.ValueKind == JsonValueKind.String)
            {
                label = string.IsNullOrWhiteSpace(labelElement.GetString()) ? null : labelElement.GetString();
            }

            return new VoiceParameterEnumValue
            {
                Value = value,
                Label = label
            };
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing {nameof(VoiceParameterEnumValue)}.");
    }

    public override void Write(Utf8JsonWriter writer, VoiceParameterEnumValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("value", value.Value);
        if (!string.IsNullOrWhiteSpace(value.Label))
        {
            writer.WriteString("label", value.Label);
        }

        writer.WriteEndObject();
    }
}
