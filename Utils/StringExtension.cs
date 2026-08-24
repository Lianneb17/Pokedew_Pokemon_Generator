namespace PokemonJsonGenerator.Utils;

public static class StringExtensions
{
    public static string? FirstCharToUpperCase(this string? input)
    {
        if (!string.IsNullOrEmpty(input) && char.IsLower(input[0]))
            return input.Length == 1 ? 
                char.ToUpper(input[0]).ToString() : 
                char.ToLower(input[0]) + input[1..];

        return input;
    }
}