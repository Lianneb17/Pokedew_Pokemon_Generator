using PokemonJsonGenerator.Models.Pokemon;
using PokemonJsonGenerator.Models.Stardew;

namespace PokemonJsonGenerator.Models;

public static class ExtensionMethods
{    
    private static readonly string[] RegionalFormNames = ["alola", "galar", "hisui", "paldea"];

    public static Group ForExport(this Group config)
    {
        return new Group
        {
            BasePokemonName = config.BasePokemonName,
            Generations = config.Generations,
            HatchCycle = config.HatchCycle,
            Levelspeed = config.Levelspeed,
            Color = config.Color,
            EggGroups = config.EggGroups,
            SpriteWidth = config.SpriteWidth,
            SpriteHeight = config.SpriteHeight,
            ShouldBeForSale = config.ShouldBeForSale,
            Evolutions = config.Evolutions,
            Pokemon = config.Pokemon.SelectMany(ExpandGenderVariants).ToList()
        };
    }

    private static IEnumerable<Pokemon.Pokemon> ExpandGenderVariants(Pokemon.Pokemon pokemon)
    {
        if (!pokemon.HasGenderVariants)
            return [pokemon];

        return [CopyWithGender(pokemon, Gender.Male), CopyWithGender(pokemon, Gender.Female)];
    }

    private static Pokemon.Pokemon CopyWithGender(Pokemon.Pokemon pokemon, Gender gender)
    {
        return new Pokemon.Pokemon
        {
            Name = pokemon.Name,
            Gender = gender,
            HasGenderVariants = true,
            BarnType = pokemon.BarnType,
            Types = pokemon.Types,
            BaseStatsTotal = pokemon.BaseStatsTotal,
        };
    }

    public static string ExportName(this Pokemon.Pokemon pokemon, Group config)
    {
        var hasGenderVariants = config.Pokemon.Any(other =>
            other.Name == pokemon.Name && other.Gender != Gender.MaleOrFemale);

        return hasGenderVariants
            ? $"{pokemon.Name}_{pokemon.Gender.ToString().ToLowerInvariant()}"
            : pokemon.Name;
    }

    public static string BasePokemon(this Group config)
    {
        if (config.Pokemon.Count == 0)
            throw new InvalidOperationException("Er is geen Pokémon toegevoegd.");

        return string.IsNullOrWhiteSpace(config.BasePokemonName)
            ? config.Pokemon[0].Name
            : config.BasePokemonName;
    }

    public static int EvolutionStage(this Pokemon.Pokemon pokemon, Group config)
    {
        if (string.Equals(pokemon.Name, config.BasePokemon(), StringComparison.OrdinalIgnoreCase))
            return 1;

        return config.Evolutions
            .Where(evolution => string.Equals(evolution.To, pokemon.Name, StringComparison.OrdinalIgnoreCase))
            .Select(evolution => evolution.Stage)
            .DefaultIfEmpty(1)
            .Max();
    }

    private static string? AlternativeTextureName(this Pokemon.Pokemon pokemon, Group config)
    {
        if (pokemon.EvolutionStage(config) < 3)
            return null;

        return config.Pokemon
            .FirstOrDefault(other => other.EvolutionStage(config) == 2)
            ?.Name;
    }

    public static LoadChange BuildLoadImages(this Group config)
    {
        var targets = "";
        if (config.ShouldBeForSale)
            targets = $"{config.BasePokemon()}/shopicon";

        foreach (var pokemon in config.Pokemon)
            targets += (string.IsNullOrEmpty(targets) ? "" : ", ")
                + $"{config.BasePokemon()}/{pokemon.ExportName(config)}, {config.BasePokemon()}/{pokemon.ExportName(config)}s";

        var fromFile = $"assets/{Constants.Folder}/{config.BasePokemon()}/{{{{TargetWithoutPath}}}}.png";

        return new LoadChange()
        {
            Target = targets,
            FromFile = fromFile,
            LogName = "Load images"
        };
    }

    public static SoundChange BuildSoundChanges(this Group config)
    {
        var soundChange = new SoundChange();

        foreach (var pokemon in config.Pokemon.GroupBy(pokemon => pokemon.Name).Select(group => group.First()))
        {
            var exportName = pokemon.Name;
            var sound = new Sound()
            {
                ID = $"{{{{modId}}}}_sound_{exportName}",
                FilePaths = [$"{{{{AbsoluteFilePath: assets/{Constants.Folder}/{config.BasePokemon()}/{exportName}.wav}}}}"]
            };

            soundChange.Entries.Add(sound.ID, sound);
        }

        return soundChange;
    }

    public static AnimalChange BuildAnimalChanges(this Group config)
    {
        var animalChange = new AnimalChange();

        foreach (var pokemon in config.Pokemon)
        {
            var exportName = pokemon.ExportName(config);
            var skin = new Skin
            {
                ID = $"{{{{modId}}}}_pokemon_{exportName}_shiny",
                Weight = 0.1,
                Texture = $"{config.BasePokemon()}/{exportName}s"
            };

            if (pokemon != config.Pokemon[0])
                skin.BabyTexture = $"{config.BasePokemon()}/{config.BasePokemon()}s";

            var animal = new Animal()
            {
                ID = $"{{{{modId}}}}_pokemon_{exportName}",
                DisplayName = $"{{{{i18n:pokemon.{exportName}}}}}",
                House = pokemon.BarnType,
                Gender = pokemon.Gender,
                EggItemIds = config.GetEggItemIds(),
                IncubationTime = config.HatchCycle.IncubationTime,
                BirthText = $"{{{{i18n:egg.all.hatching}}}}",
                DaysToMature = config.Levelspeed.DaysToMature,
                CanGetPregnant = false,
                ProduceItemIds = [new ProduceItem {
                    Id = $"{{{{modId}}}}_pokemon_produceItem_egg_{exportName}",
                    ItemID = $"{{{{modId}}}}_item_egg_{config.BasePokemon()}"
                }],
                DaysToProduce = config.HatchCycle.DaysToProduce,
                ProduceOnMature = true,
                Sound = $"{{{{modId}}}}_sound_{pokemon.Name}",
                Texture = $"{config.BasePokemon()}/{exportName}",
                BabyTexture = pokemon != config.Pokemon[0] ? $"{config.BasePokemon()}/{config.BasePokemon()}" : null,
                SpriteWidth = config.SpriteWidth,
                SpriteHeight = config.SpriteHeight,
                Skins = [skin],
                SleepFrame = 16,
                UseDoubleUniqueAnimationFrames = true,
                CanSwim = pokemon.Types.Contains(PokemonType.Water),
                SellPrice = config.HatchCycle.SellPrice
            };

            var unlockConditions = new List<string>();
            var regionCondition = pokemon.RegionCondition();
            if (regionCondition is not null)
                unlockConditions.Add(regionCondition);
            unlockConditions.AddRange(pokemon.EvolutionItemConditions(config));
            unlockConditions.AddRange(pokemon.EvolutionTradeConditions(config));

            if (pokemon == config.Pokemon[0] && config.ShouldBeForSale)
            {
                unlockConditions.Insert(0, $"{{{{{Constants.ConfigItem}}}}}");
                animal.PurchasePrice = config.HatchCycle.PurchasePrice;
                animal.ShopTexture = $"{config.BasePokemon()}/shopicon";
                animal.RequiredBuilding = pokemon.BarnType;
                animal.ShopDescription = $"{{{{i18n:pokemon.{exportName}.shop}}}}";
                animal.ShopMissingBuildingDescription = "{{i18n:pokemon.all.shop.missing}}";
                animal.ShowInSummitCredits = true;

                var purchaseGroups = config.Pokemon
                    .Select((purchasePokemon, index) => new { purchasePokemon, index })
                    .GroupBy(item => item.purchasePokemon.BarnType)
                    .OrderByDescending(group => group.Max(item => item.index));

                foreach (var purchaseGroup in purchaseGroups)
                {
                    var alternatePurchaseType = new AlternatePurchaseType
                    {
                        ID = $"{{{{modId}}}}_pokemon_purchase_{config.BasePokemon()}_{purchaseGroup.Key}",
                        AnimalIDs = purchaseGroup
                            .Select(item => $"{{{{modId}}}}_pokemon_{item.purchasePokemon.ExportName(config)}")
                            .ToList()
                    };

                    var chance = purchaseGroup.Key switch
                    {
                        BarnType.PokeBarn => 1.0,
                        BarnType.BigPokeBarn => 0.5,
                        _ => 1.0 / 3
                    };
                    if (chance < 1 && purchaseGroup.Key != BarnType.PokeBarn)
                        alternatePurchaseType.Condition += $"BUILDINGS_CONSTRUCTED All \"{purchaseGroup.Key}\", RANDOM {chance:F3}";

                    animal.AlternatePurchaseTypes.Add(alternatePurchaseType);
                }
            }

            if (unlockConditions.Count > 0)
                animal.UnlockCondition = string.Join(", ", unlockConditions);

            if (pokemon.Types.Contains(PokemonType.Electric)) {
                var battery = new ProduceItem {
                    Id = $"{{{{modId}}}}_pokemon_deluxeProduceItem_battery_{pokemon.ExportName(config)}",
                    ItemID = "787",
                    Condition = "WEATHER Here Storm"
                };
                animal.DeluxeProduceItemIds.Add(battery);
            }

            if (pokemon.Types.Contains(PokemonType.Fairy)) {
                var fairydust = new ProduceItem {
                    Id = $"{{{{modId}}}}_pokemon_deluxeProduceItem_fairydust_{pokemon.ExportName(config)}",
                    ItemID = "872"
                };
                animal.DeluxeProduceItemIds.Add(fairydust);
            }

            if (pokemon.Types.Contains(PokemonType.Flying)) {
                var duckfeather = new ProduceItem {
                    Id = $"{{{{modId}}}}_pokemon_deluxeProduceItem_duckfeather_{pokemon.ExportName(config)}",
                    ItemID = "444"
                };
                animal.DeluxeProduceItemIds.Add(duckfeather);
            }

            animalChange.Entries.Add(animal.ID, animal);
        }

        return animalChange;
    }

    public static List<string> GetEggItemIds(this Group config)
    {
        List<string> result = [$"{{{{modId}}}}_item_egg_{config.BasePokemon()}"];

        foreach(var group in config.EggGroups)
        {
            result.Add($"{{{{modId}}}}_item_egg_group_{group.ToString().ToLowerInvariant()}");
        }

        return result;
    }

    public static ExtraAnimalConfigurationChange BuildExtraAnimalConfigurationChanges(this Group config)
    {
        var result = new ExtraAnimalConfigurationChange();

        foreach(var pokemon in config.Pokemon)
        {
            var exportName = pokemon.ExportName(config);
            var extraAnimalConfig = new ExtraAnimalConfiguration {
                ID = $"{{{{modId}}}}_pokemon_{exportName}"
            };

            var hasExtras = false;

            var alternativeTextureName = pokemon.AlternativeTextureName(config);
            if (alternativeTextureName is not null)
            {
                hasExtras = true;
                extraAnimalConfig.TextureOverrides = [
                    new AppearanceData {
                        Id = $"{{{{modId}}}}_pokemon_texture_{exportName}",
                        Condition = $"selph.ExtraAnimalConfig_ANIMAL_AGE {config.Levelspeed.TextureOverrides}",
                        TextureToUse = $"{config.BasePokemon()}/{alternativeTextureName}"
                    },
                    new AppearanceData {
                        Id = $"{{{{modId}}}}_pokemon_texture_{exportName}_shiny",
                        Skin = $"{{{{modId}}}}_pokemon_{exportName}_shiny",
                        Condition = $"selph.ExtraAnimalConfig_ANIMAL_AGE {config.Levelspeed.TextureOverrides}",
                        TextureToUse = $"{config.BasePokemon()}/{alternativeTextureName}s"
                    }];
            }

            if (pokemon.Types.Contains(PokemonType.Water) 
                || pokemon.Types.Contains(PokemonType.Grass)) {
                hasExtras = true;
                extraAnimalConfig.IgnoreRain = true;
            }

            if (pokemon.Types.Contains(PokemonType.Ice)){
                hasExtras = true;
                extraAnimalConfig.IgnoreWinter = true;
            }

            if (pokemon.Types.Contains(PokemonType.Fire)){
                hasExtras = true;
                extraAnimalConfig.IsHeater = true;
            }

            if (pokemon.Types.Contains(PokemonType.Ghost)){
                hasExtras = true;
                extraAnimalConfig.GlowColor = "SlateBlue";
                extraAnimalConfig.GlowRadius = 30;
            }

            if (pokemon.Types.Contains(PokemonType.Ground)){
                hasExtras = true;
                extraAnimalConfig.ExtraProduceSpawnList.Add(new ExtraProduceSpawnData {
                        Id = $"{{{{modId}}}}_pokemon_extraSpawn_truffle_{exportName}",
                    ProduceItems = [new ProduceItem {
                        Id = $"{{{{modId}}}}_pokemon_spawn_truffle_{exportName}",
                        ItemID = "430"
                    }],
                    DaysToProduce = 1,
                    SyncWithMainProduce = false
                });
            }

            if (hasExtras)
                result.Entries.Add(extraAnimalConfig.ID, extraAnimalConfig);
        }

        return result;
    }

    public static ObjectChange BuildEggData(this Group config)
    {
        var eggData = new ObjectData {
            ID = $"{{{{modId}}}}_item_egg_{config.BasePokemon()}",
            Name = $"{{{{modId}}_item_egg_{config.BasePokemon()}",
            DisplayName = $"{{{{i18n:egg.{config.BasePokemon()}.item}}}}",
            Description = $"{{{{i18n:egg.{config.BasePokemon()}.item.description}}}}",
            Type = ObjectType.Basic,
            Category = -5,
            Price = config.HatchCycle.EggPrice,
            Texture = "eggs/color",
            SpriteIndex = config.Color.SpriteIndex,
            Edibility = config.HatchCycle.Edibility,
            ContextTags = ["egg_item", $"color_{config.Color.Color}"],
            ExcludeFromShippingCollection = true
        };

        if (config.Pokemon[0].Types.Contains(PokemonType.Fighting))
            eggData.Buffs.Add(new Buff{
                Effects = new Effects {
                    Attack = 3
                }
            });

        if (config.Pokemon[0].Types.Contains(PokemonType.Poison))
            eggData.Edibility = -100;

        return new ObjectChange
        {
            LogName = "Creating eggdata",
            Entries = new Dictionary<string, ObjectData>
            {
                {
                    eggData.ID,
                    eggData
                }
            }
        };
    }

    public static EggExtensionChange BuildEggExtensionData(this Group config)
    {
        var spawnList = new List<AnimalSpawnData>();
        var remainingWeight = config.Pokemon.Sum(RandomWeight);

        for (var i = config.Pokemon.Count - 1; i >= 0; i--)
        {
            var pokemon = config.Pokemon[i];
            var spawn = new AnimalSpawnData
            {
                Id = $"{{{{modId}}}}_egg_spawn_{pokemon.ExportName(config)}",
                AnimalId = $"{{{{modId}}}}_pokemon_{pokemon.ExportName(config)}"
            };

            var weight = RandomWeight(pokemon);
            var chance = weight / remainingWeight;
            if (chance < 1.0)
                spawn.Condition = $"RANDOM {chance.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}";

            var evolutionItemConditions = pokemon.EvolutionItemConditions(config);
            if (evolutionItemConditions.Count > 0)
                spawn.Condition = string.Join(", ", evolutionItemConditions.Append(spawn.Condition).Where(condition => condition is not null));

            var evolutionTradeConditions = pokemon.EvolutionTradeConditions(config);
            if (evolutionTradeConditions.Count > 0)
                spawn.Condition = string.Join(", ", evolutionTradeConditions.Append(spawn.Condition).Where(condition => condition is not null));

            spawnList.Add(spawn);
            remainingWeight -= weight;
        }

        return new EggExtensionChange
        {
            Entries = new Dictionary<string, EggExtension>
            {
                {
                    $"{{{{modId}}}}_item_egg_{config.BasePokemon()}",
                    new EggExtension {
                    ID = $"{{{{modId}}}}_item_egg_{config.BasePokemon()}",
                    AnimalSpawnList = spawnList
                    }
                }
            }
        };
    }

    public static DataJson UpdateEggs(this Group input, DataJson eggData)
    {
        return new[] { input }.UpdateEggs(eggData);
    }

    public static DataJson UpdateEggs(this IReadOnlyList<Group> inputs, DataJson eggData)
    {
        var eggExtensionChanges = eggData.Changes
            .OfType<EggExtensionChange>()
            .SelectMany(change => change.Entries)
            .ToDictionary(entry => entry.Key, entry => entry.Value);

        var pokemonByEggGroup = new Dictionary<string, List<(Pokemon.Pokemon Pokemon, Group Config)>>();
        foreach (var input in inputs)
        {
            foreach (var eggGroup in input.EggGroups.Distinct())
            {
                var groupName = eggGroup.ToString().ToLowerInvariant();
                if (!pokemonByEggGroup.TryGetValue(groupName, out var pokemon))
                {
                    pokemon = [];
                    pokemonByEggGroup[groupName] = pokemon;
                }

                pokemon.AddRange(input.Pokemon.Select(entry => (entry, input)));
            }
        }

        foreach (var (groupName, pokemonInGroup) in pokemonByEggGroup)
        {
            var entryId = $"{{{{modId}}}}_item_egg_group_{groupName}";
            if (!eggExtensionChanges.TryGetValue(entryId, out var eggExtension))
                continue;

            var currentListCount = eggExtension.AnimalSpawnList.Count;
            if (currentListCount == 0)
                continue;

            var remainingWeight = (double)currentListCount;
            var pokemonByLevel = pokemonInGroup
                .OrderByDescending(entry => entry.Pokemon.BaseStatsTotal.FarmLevel)
                .ToList();
            for (var i = pokemonByLevel.Count - 1; i >= 0; i--)
            {
                var entry = pokemonByLevel[i];
                var pokemon = entry.Pokemon;
                var weight = RandomWeight(pokemon);
                remainingWeight += weight;
                eggExtension.AnimalSpawnList.Insert(0, CreateGroupEgg(
                    groupName,
                    pokemon,
                    entry.Config,
                    weight / remainingWeight));
            }
        }

        var entryIdAll = "{{modId}}_item_egg_all";
        var pokemonForAll = inputs
            .Where(input => !input.EggGroups.Contains(EggGroup.NoEggDiscovered))
            .SelectMany(input => input.Pokemon.Select(pokemon => (Pokemon: pokemon, Config: input)))
            .OrderByDescending(entry => entry.Pokemon.BaseStatsTotal.FarmLevel)
            .ToList();
        if (eggExtensionChanges.TryGetValue(entryIdAll, out var eggExtensionAll))
        {
            var remainingWeight = (double)eggExtensionAll.AnimalSpawnList.Count;
            for (var i = pokemonForAll.Count - 1; i >= 0; i--)
            {
                var entry = pokemonForAll[i];
                var pokemon = entry.Pokemon;
                var weight = RandomWeight(pokemon);
                remainingWeight += weight;
                eggExtensionAll.AnimalSpawnList.Insert(0, CreateGroupEgg(
                    "all",
                    pokemon,
                    entry.Config,
                    weight / remainingWeight));
            }
        }

        return eggData;
    }

    private static double RandomWeight(Pokemon.Pokemon pokemon)
    {
        return pokemon.HasGenderVariants ? 0.5 : 1.0;
    }

    private static string? RegionCondition(this Pokemon.Pokemon pokemon)
    {
        var region = RegionalFormNames.FirstOrDefault(region =>
            pokemon.Name.EndsWith($"_{region}", StringComparison.OrdinalIgnoreCase));

        return region is null
            ? null
            : $"PLAYER_HAS_MAIL current {{{{modId}}}}_state_region_{region}_unlocked received";
    }

    private static List<string> EvolutionItemConditions(this Pokemon.Pokemon pokemon, Group config)
    {
        return [.. config.Evolutions
            .Where(evolution => string.Equals(evolution.To, pokemon.Name, StringComparison.OrdinalIgnoreCase))
            .Where(evolution => !string.IsNullOrWhiteSpace(evolution.Item))
            .Select(evolution =>
                $"PLAYER_HAS_MAIL current {{{{modId}}}}_state_evolveitem_{evolution.Item}_received received")
            .Distinct()];
    }

    private static List<string> EvolutionTradeConditions(this Pokemon.Pokemon pokemon, Group config)
    {
        return [.. config.Evolutions
            .Where(evolution => string.Equals(evolution.To, pokemon.Name, StringComparison.OrdinalIgnoreCase))
            .Where(evolution => string.Equals(evolution.Trigger, "trade", StringComparison.OrdinalIgnoreCase))
            .Select(evolution =>
                $"PLAYER_HAS_MAIL current {{{{modId}}}}_state_trade_{pokemon.Name}_completed received")
            .Distinct()];
    }

    public static AnimalSpawnData CreateGroupEgg(
        string groupName,
        Pokemon.Pokemon pokemon,
        Group config,
        double chance)
    {
        var conditions = new List<string> { config.GenerationCondition() };
        if (pokemon.BaseStatsTotal.FarmLevel > 0)
            conditions.Add($"PLAYER_BASE_FARMING_LEVEL current {pokemon.BaseStatsTotal.FarmLevel}");

        var regionCondition = pokemon.RegionCondition();
        if (regionCondition is not null)
            conditions.Add(regionCondition);
        conditions.AddRange(pokemon.EvolutionItemConditions(config));

        var spawnChance = chance;
        if (spawnChance < 1.0)
            conditions.Add($"RANDOM {spawnChance.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}");

        return new AnimalSpawnData
        {
            Id = $"{{{{modId}}}}_spawn_egg_{groupName}_{pokemon.ExportName(config)}",
            AnimalId = $"{{{{modId}}}}_pokemon_{pokemon.ExportName(config)}",
            Condition = string.Join(", ", conditions)
        };
    }

    private static string GenerationCondition(this Group config)
    {
        var generations = config.Generations
            .Select(TranslateGeneration)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (generations.Count == 0)
            throw new InvalidOperationException($"Groep '{config.BasePokemon()}' heeft geen generatie.");

        if (generations.Count == 1)
            return $"{{{{{generations[0]}}}}}";

        return $"ANY {string.Join(" ", generations.Select(generation => $"\"{{{{{generation}}}}}\""))}";
    }

    private static string TranslateGeneration(string generation) => generation.ToLowerInvariant() switch
    {
        "generation-i" => "Kanto active",
        "generation-ii" => "Johto active",
        "generation-iii" => "Hoenn active",
        "generation-iv" => "Sinnoh active",
        "generation-v" => "Unova active",
        "generation-vi" => "Kalos active",
        "generation-vii" => "Alola active",
        "generation-viii" => "Galar active",
        "generation-ix" => "Paldea active",
        _ => throw new InvalidOperationException($"Onbekende Pokémon-generatie '{generation}'.")
    };
}
