using PokemonJsonGenerator.Models;

namespace PokemonJsonGenerator;

internal static class Program
{
    public static async Task Main()
    {
        try
        {
            var input = await Questions.AskGeneratorConfig();

            var pokemonPath = Path.Combine(
                Environment.CurrentDirectory,
                "Result",
                $"{input.BasePokemon()}data.json");

            var eggPath = Path.Combine(
                Environment.CurrentDirectory,
                "Result",
                "eggdata.json");

            JsonGenerator.Write(input, pokemonPath);

            Console.WriteLine();
            Console.WriteLine("=== Klaar ===");
            Console.WriteLine($"JSON geschreven naar:");
            Console.WriteLine(pokemonPath);

            var eggData = JsonGenerator.Read(eggPath);
            JsonGenerator.Write(input.UpdateEggs(eggData), eggPath);

            Console.WriteLine(eggPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Genereren mislukt: {ex.Message}");
        }
    }
}
