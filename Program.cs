using PokemonJsonGenerator.Models;

namespace PokemonJsonGenerator;

internal static class Program
{
    public static async Task Main()
    {
        try
        {
            var databasePath = Path.Combine(Environment.CurrentDirectory, "pokemon.db");
            var database = new PokemonDatabase(databasePath);
            await database.InitializeAsync();

            Console.WriteLine("1. Pokémon ophalen uit PokeAPI");
            Console.WriteLine("2. Bestaande Pokémon exporteren");
            Console.Write("> ");

            var option = Console.ReadLine();
            if (option == "1")
            {
                do
                {
                    var input = await Questions.AskGeneratorConfig();
                    await database.SaveGroupAsync(input);

                    Console.WriteLine();
                    Console.WriteLine("=== Opgeslagen ===");
                    Console.WriteLine($"Pokémon opgeslagen in de database: {databasePath}");
                }
                while (Questions.AskAnotherPokemon());

                return;
            }

            if (option != "2")
                throw new InvalidOperationException("Ongeldige keuze.");

            var storedInput = await database.LoadGroupAsync(Questions.AskBasePokemonName());

            var pokemonPath = Path.Combine(
                Environment.CurrentDirectory,
                "Result",
                $"{storedInput.BasePokemon()}data.json");

            var eggPath = Path.Combine(
                Environment.CurrentDirectory,
                "eggdata.json");
            var resultDirectory = Path.Combine(Environment.CurrentDirectory, "Result");
            var resultEggPath = Path.Combine(resultDirectory, "eggdata.json");
            Directory.CreateDirectory(resultDirectory);

            JsonGenerator.Write(storedInput, pokemonPath);

            Console.WriteLine();
            Console.WriteLine("=== Klaar ===");
            Console.WriteLine($"JSON geschreven naar:");
            Console.WriteLine(pokemonPath);
            Console.WriteLine($"Database bijgewerkt: {databasePath}");

            var eggData = JsonGenerator.Read(eggPath);
            JsonGenerator.Write(storedInput.ForExport().UpdateEggs(eggData), resultEggPath);

            Console.WriteLine(resultEggPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Genereren mislukt: {ex.Message}");
        }
    }

}
