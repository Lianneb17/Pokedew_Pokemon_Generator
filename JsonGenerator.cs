using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections;
using System.Globalization;
using PokemonJsonGenerator.Models;

namespace PokemonJsonGenerator;

public static class JsonGenerator
{
    public static GeneratedJson Generate(GeneratorConfig config)
    {
        if (config.Pokemon.Count == 0)
            throw new InvalidOperationException("At least one Pokémon is required.");

        return config.CreateJsonObject();
    }

    public static void Write(GeneratorConfig config, string filePath)
    {
        var root = Generate(config);

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
            // Ensure a TypeInfoResolver is set before the options becomes read-only.
            // This avoids "JsonSerializerOptions instance must specify a TypeInfoResolver setting before being marked as read-only."
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
            throw new NotSupportedException("Changes are only serialized.");
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
            throw new NotSupportedException("Boolean values are only serialized.");
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
            throw new NotSupportedException("Numeric values are only serialized.");
        }
    }
}
