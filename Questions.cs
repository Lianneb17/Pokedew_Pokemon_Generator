using PokemonJsonGenerator.Models;

namespace PokemonJsonGenerator;

public static class Questions
{
    public static GeneratorConfig AskGeneratorConfig()
    {
        Console.WriteLine("=== Pokémon JSON Generator ===");
        Console.WriteLine();

        var config = new GeneratorConfig
        {
            HatchCycle = AskChoice("HatchCycle", Datasets.HatchCycles,
                x => $"{x.HatchCycle} dagen | Purchase {x.PurchasePrice} | Sell {x.SellPrice} | Incubation {x.IncubationTime} | Produce {x.DaysToProduce}"),

            Levelspeed = AskChoice("Levelspeed", Datasets.Levelspeeds,
                x => $"{x.Levelspeed} | DaysToMature {x.DaysToMature} | Textures {x.TextureOverrides}"),

            Color = AskChoice("Color", Datasets.Colors,
                x => $"{x.Color} | SpriteIndex {x.SpriteIndex}"),

            Groups = AskMultipleChoice("Groups", Datasets.EggGroups, x => x.ToString()),

            SpriteWidth = AskPositiveInt("Sprite width"),
            SpriteHeight = AskPositiveInt("Sprite height")
        };

        Console.WriteLine();
        Console.WriteLine("=== Pokémon ===");

        do
        {
            config.Pokemon.Add(AskPokemon(config.Pokemon));
            Console.WriteLine();
        }
        while (AskYesNo("Nog een Pokémon toevoegen?"));

        return config;
    }

    private static PokemonConfig AskPokemon(IReadOnlyList<PokemonConfig> existingPokemon)
    {
        var pokemon = new PokemonConfig
        {
            Name = AskString("Naam"),
            Gender = AskChoice("Gender", Enum.GetValues<Gender>(), x => x.ToString()),
            BarnType = AskChoice("Barn type", Enum.GetValues<BarnType>(), x => x.ToString()),
            Types = AskMultipleChoice("Types", Enum.GetValues<PokemonType>(),
                x => x.ToString().ToLowerInvariant(), 2),
            HasExtraTexture = AskYesNo("Heeft deze Pokémon een extra texture?")
        };

        if (pokemon.HasExtraTexture)
        {
            if (existingPokemon.Count == 0)
                throw new InvalidOperationException(
                    "Een alternative texture kan niet worden gekozen voordat er een eerdere Pokémon bestaat.");

            pokemon.AlternativeTextureIndex = AskChoice(
                "Alternative texture",
                Enumerable.Range(0, existingPokemon.Count).ToArray(),
                index => $"{index}: {existingPokemon[index].Name}");
        }

        return pokemon;
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
