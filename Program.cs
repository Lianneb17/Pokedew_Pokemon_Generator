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
            Console.WriteLine("3. Database beheren");
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

            if (option == "3")
            {
                await Questions.ManageDatabaseAsync(database);
                return;
            }

            if (option != "2")
                throw new InvalidOperationException("Ongeldige keuze.");

            var eggPath = Path.Combine(
                Environment.CurrentDirectory,
                "eggdata.json");
            var resultDirectory = Path.Combine(Environment.CurrentDirectory, "Result");
            var resultEggPath = Path.Combine(resultDirectory, "eggdata.json");
            Directory.CreateDirectory(resultDirectory);

            var storedGroups = await database.LoadGroupsAsync();
            foreach (var storedGroup in storedGroups)
            {
                var groupPokemonPath = Path.Combine(
                    resultDirectory,
                    $"{storedGroup.BasePokemon()}data.json");

                JsonGenerator.Write(storedGroup, groupPokemonPath);
                Console.WriteLine($"JSON geschreven naar: {groupPokemonPath}");
            }

            var eggData = JsonGenerator.Read(eggPath);
            var exportGroups = storedGroups.Select(group => group.ForExport()).ToList();
            exportGroups.UpdateEggs(eggData);

            JsonGenerator.Write(eggData, resultEggPath);

            Console.WriteLine();
            Console.WriteLine("=== Klaar ===");
            Console.WriteLine(resultEggPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Genereren mislukt: {ex.Message}");
        }
    }

}
