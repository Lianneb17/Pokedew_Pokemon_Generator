using PokemonJsonGenerator.Models;
using PokemonJsonGenerator.Models.Pokemon;

namespace PokemonJsonGenerator;

public static class Questions
{
    public static async Task<Group> AskGeneratorConfig()
    {
        Console.WriteLine("=== Pokémon JSON Generator ===");
        Console.WriteLine();

        Console.WriteLine("=== Pokémon API ===");
        var pokemonName = AskString("Basis-Pokémon");
        var config = await new PokeApiClient().BuildGroupAsync(pokemonName);

        Console.WriteLine($"{config.Pokemon.Count} Pokémon-entries opgehaald uit PokeAPI.");

        return config;
    }

    public static bool AskAnotherPokemon() => AskYesNo("Nog een Pokémon ophalen?");

    public static async Task ManageDatabaseAsync(PokemonDatabase database)
    {
        var groups = await database.LoadGroupsAsync();
        if (groups.Count == 0)
        {
            Console.WriteLine("Er zijn nog geen Pokémon-groepen opgeslagen.");
            return;
        }

        var group = AskChoice(groups, "Kies een Pokémon-groep", item => item.BasePokemonName);
        Console.WriteLine();
        Console.WriteLine("1. Pokémon bewerken");
        Console.WriteLine("2. Pokémon verwijderen");
        Console.WriteLine("3. Hele groep verwijderen");
        Console.Write("> ");

        switch (Console.ReadLine())
        {
            case "1":
                await EditPokemonAsync(database, group);
                break;
            case "2":
                await DeletePokemonAsync(database, group);
                break;
            case "3":
                if (AskYesNo($"Groep '{group.BasePokemonName}' volledig verwijderen?"))
                {
                    await database.DeleteGroupAsync(group.BasePokemonName);
                    Console.WriteLine("Groep verwijderd.");
                }
                break;
            default:
                Console.WriteLine("Ongeldige keuze.");
                break;
        }
    }

    private static async Task EditPokemonAsync(PokemonDatabase database, Group group)
    {
        var pokemon = AskChoice(group.Pokemon, "Kies een Pokémon", item => $"{item.Name} ({item.Gender})");
        var editing = true;

        while (editing)
        {
            Console.WriteLine();
            Console.WriteLine($"=== {pokemon.Name} bewerken ===");
            Console.WriteLine("1. Naam");
            Console.WriteLine("2. Geslacht");
            Console.WriteLine("3. Barn-type");
            Console.WriteLine("4. Pokémon-types");
            Console.WriteLine("5. Totale basisstats");
            Console.WriteLine("6. Farm-level");
            Console.WriteLine("7. Opslaan en klaar");
            Console.Write("> ");

            switch (Console.ReadLine())
            {
                case "1":
                    var name = AskString("Nieuwe naam");
                    if (group.Pokemon.Any(item => item != pokemon &&
                        string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.WriteLine("Deze naam bestaat al in de groep.");
                    }
                    else
                    {
                        pokemon.Name = name;
                    }
                    break;
                case "2":
                    pokemon.Gender = AskChoice("Geslacht", Enum.GetValues<Gender>(), value => value.ToString());
                    pokemon.HasGenderVariants = pokemon.Gender == Gender.MaleOrFemale;
                    break;
                case "3":
                    pokemon.BarnType = AskChoice("Barn-type", Enum.GetValues<BarnType>(), value => value.ToString());
                    break;
                case "4":
                    pokemon.Types = AskMultipleChoice("Pokémon-types", Enum.GetValues<PokemonType>(), value => value.ToString());
                    break;
                case "5":
                    pokemon.BaseStatsTotal = pokemon.BaseStatsTotal with
                    {
                        BaseStatsTotal = AskNonNegativeInt("Totale basisstats")
                    };
                    break;
                case "6":
                    pokemon.BaseStatsTotal = pokemon.BaseStatsTotal with
                    {
                        FarmLevel = AskNonNegativeInt("Farm-level")
                    };
                    break;
                case "7":
                    editing = false;
                    break;
                default:
                    Console.WriteLine("Ongeldige keuze.");
                    break;
            }
        }

        await database.SaveGroupAsync(group);
        Console.WriteLine("Pokémon aangepast en opgeslagen.");
    }

    private static async Task DeletePokemonAsync(PokemonDatabase database, Group group)
    {
        if (group.Pokemon.Count == 1)
        {
            Console.WriteLine("De laatste Pokémon kan niet los worden verwijderd. Verwijder de hele groep.");
            return;
        }

        var pokemonToDelete = AskMultipleChoice(
            "Kies Pokémon om te verwijderen",
            group.Pokemon,
            item => $"{item.Name} ({item.Gender})",
            group.Pokemon.Count - 1);
        var names = string.Join(", ", pokemonToDelete.Select(item => item.Name));
        if (!AskYesNo($"Deze Pokémon verwijderen: {names}?"))
            return;

        group.Pokemon.RemoveAll(pokemonToDelete.Contains);
        await database.SaveGroupAsync(group);
        Console.WriteLine("Pokémon verwijderd en opgeslagen.");
    }

    private static T AskChoice<T>(string question, IReadOnlyList<T> options, Func<T, string> display)
    {
        while (true)
        {
            Console.WriteLine(question + ":");
            for (var i = 0; i < options.Count; i++)
                Console.WriteLine($"  {i + 1}. {display(options[i])}");

            Console.Write("> ");
            if (int.TryParse(Console.ReadLine(), out var choice) &&
                choice >= 1 && choice <= options.Count)
                return options[choice - 1];

            Console.WriteLine("Ongeldige keuze.");
            Console.WriteLine();
        }
    }

    private static T AskChoice<T>(IReadOnlyList<T> options, string question, Func<T, string> display) =>
        AskChoice(question, options, display);

    private static List<T> AskMultipleChoice<T>(
        string question, IReadOnlyList<T> options, Func<T, string> display, int? maximum = null)
    {
        while (true)
        {
            var suffix = maximum is int max
                ? $" (kies maximaal {max}, bijvoorbeeld 1,3)"
                : " (meerdere keuzes mogelijk, bijvoorbeeld 1,3,5)";
            Console.WriteLine(question + suffix);
            for (var i = 0; i < options.Count; i++)
                Console.WriteLine($"  {i + 1}. {display(options[i])}");

            Console.Write("> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Kies minimaal één optie.");
                continue;
            }

            var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var indexes = new List<int>();
            var valid = true;

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var number) || number < 1 || number > options.Count)
                {
                    valid = false;
                    break;
                }

                var index = number - 1;
                if (!indexes.Contains(index))
                    indexes.Add(index);
            }

            if (valid && (maximum is not int limit || indexes.Count <= limit))
                return indexes.Select(i => options[i]).ToList();

            if (valid)
                Console.WriteLine($"Kies maximaal {maximum} opties.");
            else
                Console.WriteLine("Ongeldige keuze.");
            Console.WriteLine();
        }
    }

    private static string AskString(string question)
    {
        while (true)
        {
            Console.Write($"{question}: ");
            var value = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                return value.ToLowerInvariant();

            Console.WriteLine("Dit veld mag niet leeg zijn.");
        }
    }

    private static int AskPositiveInt(string question)
    {
        while (true)
        {
            Console.Write($"{question}: ");
            if (int.TryParse(Console.ReadLine(), out var value) && value > 0)
                return value;

            Console.WriteLine("Voer een positief geheel getal in.");
        }
    }

    private static int AskNonNegativeInt(string question)
    {
        while (true)
        {
            Console.Write($"{question}: ");
            if (int.TryParse(Console.ReadLine(), out var value) && value >= 0)
                return value;

            Console.WriteLine("Voer nul of een positief geheel getal in.");
        }
    }

    private static bool AskYesNo(string question)
    {
        while (true)
        {
            Console.Write($"{question} (j/n): ");
            var value = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (value is "j" or "ja" or "y" or "yes") return true;
            if (value is "n" or "nee" or "no") return false;

            Console.WriteLine("Antwoord met j of n.");
        }
    }
}
