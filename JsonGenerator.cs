using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PokemonJsonGenerator;

public static class JsonGenerator
{
    public static JsonObject Generate(GeneratorConfig config, string modId)
    {
        if (config.Pokemon.Count == 0)
            throw new InvalidOperationException("At least one Pokémon is required.");

        var root = new JsonObject
        {
            ["Changes"] = new JsonArray(
                BuildLoadImages(modId, config),
                BuildSounds(modId, config),
                BuildAnimals(modId, config),
                BuildExtraTextures(modId, config),
                BuildEggs(modId, config),
                BuildEggExtensions(modId, config))
        };

        return root;
    }

    public static void Write(GeneratorConfig config, string modId, string filePath)
    {
        var root = Generate(config, modId);

        var options = new JsonSerializerOptions
        {

            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            // Ensure a TypeInfoResolver is set before the options becomes read-only.
            // This avoids "JsonSerializerOptions instance must specify a TypeInfoResolver setting before being marked as read-only."
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()

        };

        File.WriteAllText(filePath, root.ToJsonString(options));
    }

    private static JsonObject BuildLoadImages(string modId, GeneratorConfig config)
    {
        var targets = new List<string>();

        var firstPokemon = config.Pokemon[0].Name;

        foreach (var (pokemon, index) in config.Pokemon.Select((p, i) => (p, i)))
        {
            var name = pokemon.Name;

            if (index == 0)
                targets.Add($"{firstPokemon}/shopicon");

            targets.Add($"{firstPokemon}/{name}");
            targets.Add($"{firstPokemon}/{name}s");
        }

        return new JsonObject
        {
            ["LogName"] = "Load images",
            ["Action"] = "Load",
            ["Target"] = string.Join(", ", targets),
            ["FromFile"] = $"assets/{firstPokemon}/{{{{TargetWithoutPath}}}}.png"
        };
    }

    private static JsonObject BuildSounds(string modId, GeneratorConfig config)
    {
        var entries = new JsonObject();
        var firstPokemonFolder = config.Pokemon[0].Name;

        foreach (var pokemon in config.Pokemon)
        {
            var id = pokemon.Name;

            entries[$"{modId}_sound_{id}"] = new JsonObject
            {
                ["ID"] = $"{modId}_sound_{id}",
                ["Category"] = "Sound",
                ["FilePaths"] = new JsonArray(
                    $"{{{{AbsoluteFilePath: assets/{firstPokemonFolder}/{id}.wav}}}}")
            };
        }

        return new JsonObject
        {
            ["LogName"] = "Sound",
            ["Action"] = "EditData",
            ["Target"] = "Data/AudioChanges",
            ["Entries"] = entries
        };
    }

    private static JsonObject BuildAnimals(string modId, GeneratorConfig config)
    {
        var entries = new JsonObject();

        for (var i = 0; i < config.Pokemon.Count; i++)
        {
            var pokemon = config.Pokemon[i];
            entries[$"{modId}_pokemon_{pokemon.Name}"] = BuildAnimal(modId, config, pokemon, i);
        }

        return new JsonObject
        {
            ["LogName"] = "Animal",
            ["Action"] = "EditData",
            ["Target"] = "Data/FarmAnimals",
            ["Entries"] = entries
        };
    }

    private static JsonObject BuildAnimal(
        string modId,
        GeneratorConfig config,
        PokemonConfig pokemon,
        int index)
    {
        var name = pokemon.Name;
        var baseName = config.Pokemon[0].Name;

        var entry = new JsonObject
        {
            ["DisplayName"] = $"{{{{i18n:pokemon.{name}}}}}",
            ["House"] = pokemon.BarnType.ToString(),
            ["Gender"] = pokemon.Gender.ToString(),
            ["EggItemIds"] = BuildEggItemIds(modId, config),
            ["IncubationTime"] = config.HatchCycle.IncubationTime.ToString(),
            ["IncubatorParentSheetOffset"] = "1",
            ["BirthText"] = "{{i18n:egg.all.hatching}}",
            ["DaysToMature"] = config.Levelspeed.DaysToMature.ToString(),
            ["CanGetPregnant"] = "False",
            ["ProduceItemIds"] = new JsonArray(
                new JsonObject
                {
                    ["Id"] = "Default",
                    ["Condition"] = null,
                    ["MinimumFriendship"] = "0",
                    ["ItemId"] = $"{modId}_item_egg_{baseName}"
                }),
            ["DaysToProduce"] = config.HatchCycle.DaysToProduce.ToString(),
            ["ProduceOnMature"] = "True",
            ["Sound"] = $"{modId}_sound_{name}",
            ["Texture"] = $"{baseName}/{name}",
            ["UseFlippedRightForLeft"] = "False",
            ["SpriteWidth"] = config.SpriteWidth.ToString(),
            ["SpriteHeight"] = config.SpriteHeight.ToString(),
            ["Skins"] = BuildSkins(modId, pokemon, baseName, index == 0),
            ["SleepFrame"] = "16",
            ["UseDoubleUniqueAnimationFrames"] = "True",
            ["CanSwim"] = "False",
            ["SellPrice"] = config.HatchCycle.SellPrice.ToString(),
            ["ShowInSummitCredits"] = (index == 0).ToString()
        };

        if (index == 0)
        {
            entry["AlternatePurchaseTypes"] = BuildAlternatePurchaseTypes(modId, config, index);
            entry["PurchasePrice"] = config.HatchCycle.PurchasePrice.ToString();
            entry["ShopTexture"] = $"{baseName}/shopicon";
            entry["RequiredBuilding"] = "PokeBarn";
            entry["ShopDescription"] = $"{{{{i18n:pokemon.{name}.shop}}}}";
            entry["ShopMissingBuildingDescription"] = "{{i18n:pokemon.all.missingbuilding}}";
        }

        if (index > 0)
            entry["BabySound"] = $"{modId}_sound_{baseName}";

        if (index > 0)
            entry["BabyTexture"] = $"{baseName}/{baseName}";

        return entry;
    }

    private static JsonArray BuildAlternatePurchaseTypes(
        string modId,
        GeneratorConfig config,
        int currentIndex)
    {
        var result = new JsonArray();

        for (var i = config.Pokemon.Count - 1; i >= 0; i--)
        {
            var pokemon = config.Pokemon[i];
            var pokemonName = pokemon.Name;
            var entry = new JsonObject
            {
                ["ID"] = $"{modId}_pokemon_purchase_{pokemonName}",
                ["AnimalId"] = $"{modId}_pokemon_{pokemonName}"
            };

            if (i != 0)
            {
                var chance = 1.0 / (i + 1);
                var building = pokemon.BarnType.ToString();
                entry["Condition"] = $"BUILDINGS_CONSTRUCTED All \"{building}\", RANDOM {chance:0.###}";
            }

            result.Add(entry);
        }

        return result;
    }

    private static JsonArray BuildEggItemIds(
        string modId,
        GeneratorConfig config)
    {
        var firstPokemonName = config.Pokemon[0].Name;
        var ids = new JsonArray
        {
            $"{modId}_item_egg_{firstPokemonName}"
        };

        foreach (var group in config.Groups)
            ids.Add($"{modId}_item_egg_group_{ToGroupId(group)}");

        return ids;
    }

    private static string ToGroupId(EggGroup group) =>
        group switch
        {
            EggGroup.HumanLike => "human-like",
            EggGroup.NoEggDiscovered => "noeggdiscovered",
            EggGroup.GenderUnknown => "genderunknown",
            _ => group.ToString().ToLowerInvariant()
        };

    private static JsonArray BuildSkins(string modId, PokemonConfig pokemon, string? firstPokemonFolder = null, bool isFirstPokemon = false)
    {
        var pokemonName = pokemon.Name;
        var assetFolder = firstPokemonFolder ?? pokemon.Name;
        var skin = new JsonObject
        {
            ["ID"] = $"{modId}_pokemon_{pokemonName}_shiny",
            ["Weight"] = "0.1",
            ["Texture"] = $"{assetFolder}/{pokemonName}s"
        };

        if (!isFirstPokemon)
            skin["BabyTexture"] = $"{assetFolder}/{assetFolder}s";

        return new JsonArray(skin);
    }

    private static JsonObject BuildExtraTextures(string modId, GeneratorConfig config)
    {
        var entries = new JsonObject();

        for (var i = 0; i < config.Pokemon.Count; i++)
        {
            var pokemon = config.Pokemon[i];

            if (!pokemon.HasExtraTexture ||
                pokemon.AlternativeTextureIndex is not int alternativeIndex)
                continue;

            var pokemonName = pokemon.Name;
            var firstPokemonFolder = config.Pokemon[0].Name;
            var alternative = config.Pokemon[alternativeIndex];
            var alternativeName = alternative.Name;

            var overrides = new JsonArray
            {
                new JsonObject
                {
                    ["Id"] = $"{modId}_texture_{pokemonName}",
                    ["Condition"] = $"selph.ExtraAnimalConfig_ANIMAL_AGE {config.Levelspeed.TextureOverrides}",
                    ["TextureToUse"] = $"{firstPokemonFolder}/{alternativeName}",
                    ["DefaultTextureToUse"] = "BabyTexture"
                },
                new JsonObject
                {
                    ["Id"] = $"{modId}_texture_{pokemonName}_shiny",
                    ["Skin"] = $"{modId}_pokemon_{pokemonName}_shiny",
                    ["Condition"] = $"selph.ExtraAnimalConfig_ANIMAL_AGE {config.Levelspeed.TextureOverrides}",
                    ["TextureToUse"] = $"{firstPokemonFolder}/{alternativeName}s",
                    ["DefaultTextureToUse"] = "BabyTexture"
                }
            };

            entries[$"{modId}_pokemon_{pokemonName}"] = new JsonObject
            {
                ["TextureOverrides"] = overrides
            };
        }

        return new JsonObject
        {
            ["LogName"] = "Extra textures",
            ["Action"] = "EditData",
            ["Target"] = "selph.ExtraAnimalConfig/AnimalExtensionData",
            ["Entries"] = entries
        };
    }

    private static JsonObject BuildEggs(string modId, GeneratorConfig config)
    {
        var entries = new JsonObject();
        var name = config.Pokemon[0].Name;

        entries[$"{modId}_item_egg_{name}"] = new JsonObject
        {
            ["Name"] = $"{modId}_item_egg_{name}",
            ["DisplayName"] = $"{{{{i18n:egg.{name}.item}}}}",
            ["Description"] = $"{{{{i18n:egg.{name}.item.description}}}}",
            ["Type"] = "Basic",
            ["Category"] = "-5",
            ["Price"] = config.HatchCycle.SellPrice.ToString(),
            ["Texture"] = "eggs/color",
            ["SpriteIndex"] = config.Color.SpriteIndex.ToString(),
            ["Edibility"] = config.HatchCycle.Edibility.ToString(),
            ["IsDrink"] = "False",
            ["Buffs"] = "",
            ["ContextTags"] = new JsonArray("egg_item", $"color_{config.Color.Color}"),
            ["ExcludeFromShippingCollection"] = "True"
        };

        return new JsonObject
        {
            ["LogName"] = "Egg",
            ["Action"] = "EditData",
            ["Target"] = "data/objects",
            ["Entries"] = entries
        };
    }

    private static JsonObject BuildEggExtensions(string modId, GeneratorConfig config)
    {
        var entries = new JsonObject();
        var name = config.Pokemon[0].Name;
        var spawnList = new JsonArray();

        // Highest index first, as in the original template.
        for (var i = config.Pokemon.Count - 1; i >= 1; i--)
        {
            var candidate = config.Pokemon[i];
            var candidateName = candidate.Name;
            var chance = 1.0 / (i + 1);
            var building = candidate.BarnType.ToString();

            spawnList.Add(new JsonObject
            {
                ["Id"] = $"{modId}_egg_spawn_{candidateName}",
                ["AnimalId"] = $"{modId}_pokemon_{candidateName}",
                ["Condition"] = $"BUILDINGS_CONSTRUCTED All \"{building}\", RANDOM {chance:0.###}"
            });
        }

        spawnList.Add(new JsonObject
        {
            ["Id"] = $"{modId}_egg_spawn_{name}",
            ["AnimalId"] = $"{modId}_pokemon_{name}"
        });

        entries[$"{modId}_item_egg_{name}"] = new JsonObject
        {
            ["AnimalSpawnList"] = spawnList
        };

        return new JsonObject
        {
            ["LogName"] = "Egg extension",
            ["Action"] = "EditData",
            ["Target"] = "selph.ExtraAnimalConfig/EggExtensionData",
            ["Entries"] = entries
        };
    }

}
