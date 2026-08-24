using PokemonJsonGenerator.Models;
using PokemonJsonGenerator.Utils;

namespace PokemonJsonGenerator;

internal static class Program
{
    public static void Main()
    {
        var input = Questions.AskGeneratorConfig();

        var pokemonPath = Path.Combine(
            Environment.CurrentDirectory,
            "Result",
            $"{input.BasePokemon().FirstCharToUpperCase()}Data.json");

        try
        {
            JsonGenerator.Write(input, pokemonPath);

            Console.WriteLine();
            Console.WriteLine("=== Klaar ===");
            Console.WriteLine($"JSON geschreven naar:");
            Console.WriteLine(pokemonPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Genereren mislukt: {ex.Message}");
        }
    }
}
