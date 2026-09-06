using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections;
using System.Globalization;
using PokemonJsonGenerator.Models;
using PokemonJsonGenerator.Models.Stardew;
using PokemonJsonGenerator.Models.Pokemon;

namespace PokemonJsonGenerator;

public static class JsonGenerator
{
    public static DataJson Read(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new ChangeJsonConverter(),
                new BooleanAsStringConverter(),
                new NumberAsStringConverterFactory(),
                new JsonStringEnumConverter()
            }
        };

        return JsonSerializer.Deserialize<DataJson>(json, options)
            ?? throw new InvalidOperationException($"Unable to deserialize JSON from '{filePath}'.");
    }

    public static DataJson GenerateStardewPokemon(Group config)
    {
        config = config.ForExport();

        if (config.Pokemon.Count == 0)
            throw new InvalidOperationException("At least one Pokémon is required.");

        var result = new DataJson
        {
            Changes = [
                config.BuildLoadImages(),
                config.BuildSoundChanges(),
                config.BuildAnimalChanges(),
                config.BuildEggData(),
                config.BuildEggExtensionData()
            ]
        };

        var extraAnimalConfiguration = config.BuildExtraAnimalConfigurationChanges();
        if (extraAnimalConfiguration.Entries.Count != 0)
            result.Changes.Add(extraAnimalConfiguration);

        return result;
    }

    public static void Write(Group config, string filePath)
    {
        Write(GenerateStardewPokemon(config), filePath);
    }

    public static void Write(DataJson root, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new ChangeJsonConverter(),
                new BooleanAsStringConverter(),
                new NumberAsStringConverterFactory(),
                new JsonStringEnumConverter()
            },
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
            {
                Modifiers = { IgnoreEmptyValues }
            }
        };

        File.WriteAllText(filePath, JsonSerializer.Serialize(root, options));
    }

    private static void IgnoreEmptyValues(System.Text.Json.Serialization.Metadata.JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties)
        {
            if (property.PropertyType == typeof(string))
                property.ShouldSerialize = (_, value) => !string.IsNullOrEmpty(value as string);
            else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                property.ShouldSerialize = (_, value) => value is IEnumerable collection && collection.GetEnumerator().MoveNext();
        }
    }

    private sealed class ChangeJsonConverter : JsonConverter<Change>
    {
        public override void Write(Utf8JsonWriter writer, Change value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        public override Change Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var element = document.RootElement;

            var target = element.TryGetProperty("Target", out var targetElement)
                ? targetElement.GetString()
                : null;

            var normalizedTarget = target?.Replace('\\', '/');

            if (string.Equals(normalizedTarget, "Data/AudioChanges", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<SoundChange>(element.GetRawText(), options)!;

            if (string.Equals(normalizedTarget, "Data/AnimalChanges", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<AnimalChange>(element.GetRawText(), options)!;

            if (string.Equals(normalizedTarget, "Data/ObjectChanges", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedTarget, "data/objects", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<ObjectChange>(element.GetRawText(), options)!;

            if (string.Equals(normalizedTarget, "selph.ExtraAnimalConfig/AnimalExtensionData", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<ExtraAnimalConfigurationChange>(element.GetRawText(), options)!;

            if (string.Equals(normalizedTarget, "selph.ExtraAnimalConfig/EggExtensionData", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<EggExtensionChange>(element.GetRawText(), options)!;

            if (element.TryGetProperty("FromFile", out _))
                return JsonSerializer.Deserialize<LoadChange>(element.GetRawText(), options)!;

            throw new NotSupportedException($"Unsupported change target '{target ?? "unknown"}'.");
        }
    }

    private sealed class BooleanAsStringConverter : JsonConverter<bool>
    {
        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value ? "True" : "False");
        }

        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                if (bool.TryParse(value, out var parsed))
                    return parsed;

                if (string.Equals(value, "True", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(value, "False", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (reader.TokenType == JsonTokenType.True)
                return true;
            if (reader.TokenType == JsonTokenType.False)
                return false;

            throw new JsonException($"Unable to read boolean value from token '{reader.TokenType}'.");
        }
    }

    private sealed class NumberAsStringConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var type = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

            return type == typeof(byte) ||
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(decimal);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(NumberAsStringConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }

    private sealed class NumberAsStringConverter<T> : JsonConverter<T>
    {
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            object? value;
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                    return default!;

                value = Convert.ChangeType(stringValue, targetType, CultureInfo.InvariantCulture);
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                value = reader.GetDecimal();
                value = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            else
            {
                throw new JsonException($"Unable to read numeric value from token '{reader.TokenType}'.");
            }

            return (T)value!;
        }
    }
}
