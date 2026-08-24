using PokemonJsonGenerator.Models.Pokemon;
using PokemonJsonGenerator.Models.Stardew;

namespace PokemonJsonGenerator.Models;

public static class ExtensionMethods
{
    public static DataJson CreateJsonObject(this Group config)
    {
        var result = new DataJson
        {
            Changes = [
                config.BuildLoadImages(),
                config.BuildSoundChanges(),
                config.BuildAnimalChanges(),
                config.BuildEggData(),
                config.BuildEggExtensionData()
            ]
        };

        var extraAnimalConfiguration = config.BuildExtraAnimalConfigurationChanges();
        if (extraAnimalConfiguration.Entries.Count != 0)
            result.Changes.Add(extraAnimalConfiguration);

        return result;
    }
    public static string BasePokemon(this Group config)
    {
        if (config.Pokemon.Count == 0)
            throw new InvalidOperationException("Er is geen Pokémon toegevoegd.");

        return config.Pokemon[0].Name;
    }

    public static LoadChange BuildLoadImages(this Group config)
    {
        var targets = $"{config.BasePokemon()}/shopicon";

        foreach (var pokemon in config.Pokemon)
        {
            targets += $", {config.BasePokemon()}/{pokemon.Name}, {config.BasePokemon()}/{pokemon.Name}s";
        }

        var fromFile = $"assets/{config.BasePokemon()}/{{{{TargetWithoutPath}}}}.png";

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

        foreach (var pokemon in config.Pokemon)
        {
            var sound = new Sound()
            {
                ID = $"{{{{modId}}}}_sound_{pokemon.Name}",
                FilePaths = [$"{{{{AbsoluteFilePath: assets/{config.BasePokemon()}/{pokemon.Name}.wav}}}}"]
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
            var skin = new Skin
            {
                ID = $"{{{{modId}}}}_pokemon_{pokemon.Name}_shiny",
                Weight = 0.1,
                Texture = $"{config.BasePokemon()}/{pokemon.Name}s"
            };

            if (pokemon != config.Pokemon[0])
                skin.BabyTexture = $"{config.BasePokemon()}/{config.BasePokemon()}";

            var animal = new Animal()
            {
                ID = $"{{{{modId}}}}_pokemon_{pokemon.Name}",
                DisplayName = $"{{{{i18n:pokemon.{pokemon.Name}}}}}",
                House = pokemon.BarnType,
                Gender = pokemon.Gender,
                EggItemIds = config.GetEggItemIds(),
                IncubationTime = config.HatchCycle.IncubationTime,
                BirthText = $"{{{{i18n:egg.all.hatching}}}}",
                DaysToMature = config.Levelspeed.DaysToMature,
                CanGetPregnant = false,
                ProduceItemIds = [new ProduceItem {
                    Id = $"{{{{modId}}}}_pokemon_produceItem_egg_{pokemon.Name}",
                    ItemID = $"{{{{modId}}}}_item_egg_{config.BasePokemon()}"
                }],
                DaysToProduce = config.HatchCycle.DaysToProduce,
                ProduceOnMature = true,
                Sound = $"{{{{modId}}}}_sound_{pokemon.Name}",
                Texture = $"{pokemon.Name}/{pokemon.Name}",
                SpriteWidth = config.SpriteWidth,
                SpriteHeight = config.SpriteHeight,
                Skins = [skin],
                SleepFrame = 16,
                UseDoubleUniqueAnimationFrames = true,
                CanSwim = pokemon.Types.Contains(PokemonType.Water),
                SellPrice = config.HatchCycle.SellPrice
            };

            if (pokemon == config.Pokemon[0])
            {
                animal.PurchasePrice = config.HatchCycle.PurchasePrice;
                animal.ShopTexture = $"{config.BasePokemon()}/shopicon";
                animal.RequiredBuilding = pokemon.BarnType;
                animal.ShopDescription = $"{{{{i18n:pokemon.{pokemon.Name}.shop}}}}";
                animal.ShopMissingBuildingDescription = "{{i18n:pokemon.all.shop.missing}}";
                animal.ShowInSummitCredits = true;

                for (var i = config.Pokemon.Count - 1; i >= 0; i--)
                {
                    var alternatePurchaseType = new AlternatePurchaseType
                    {
                        ID = $"{{{{modId}}}}_pokemon_purchase_{config.Pokemon[i].Name}",
                        AnimalIDs = [$"{{{{modId}}}}_pokemon_{config.Pokemon[i].Name}"]
                    };

                    var chance = 1.0 / (i + 1);
                    var purchasePokemon = config.Pokemon[i];
                    if (chance < 1 && purchasePokemon.BarnType != BarnType.PokeBarn)
                        alternatePurchaseType.Condition += $"BUILDINGS_CONSTRUCTED All \"{purchasePokemon.BarnType}\", RANDOM {chance:F3}";

                    animal.AlternatePurchaseTypes.Add(alternatePurchaseType);
                }
            }

            if (pokemon.Types.Contains(PokemonType.Electric)) {
                var battery = new ProduceItem {
                    Id = $"{{{{modId}}}}_pokemon_deluxeProduceItem_battery_{pokemon.Name}",
                    ItemID = "787",
                    Condition = "WEATHER Here Storm"
                };
                animal.DeluxeProduceItemIds.Add(battery);
            }

            if (pokemon.Types.Contains(PokemonType.Fairy)) {
                var fairydust = new ProduceItem {
                    Id = $"{{{{modId}}}}_pokemon_deluxeProduceItem_fairydust_{pokemon.Name}",
                    ItemID = "872"
                };
                animal.DeluxeProduceItemIds.Add(fairydust);
            }

            if (pokemon.Types.Contains(PokemonType.Flying)) {
                var duckfeather = new ProduceItem {
                    Id = $"{{{{modId}}}}_pokemon_deluxeProduceItem_duckfeather_{pokemon.Name}",
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

        foreach(var group in config.Groups)
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
            var extraAnimalConfig = new ExtraAnimalConfiguration {
                ID = $"{{{{modId}}}}_pokemon_{pokemon.Name}"
            };

            var hasExtras = false;

            if (pokemon.HasExtraTexture)
            {
                hasExtras = true;
                extraAnimalConfig.TextureOverrides = [
                    new AppearanceData {
                        Id = $"{{{{modId}}}}_pokemon_texture_{pokemon.Name}",
                        Condition = $"selph.ExtraAnimalConfig_ANIMAL_AGE {config.Levelspeed.TextureOverrides}",
                        TextureToUse = $"{config.BasePokemon()}/{pokemon.AlternativeTextureIndex}"
                    },
                    new AppearanceData {
                        Id = $"{{{{modId}}}}_pokemon_texture_{pokemon.Name}_shiny",
                        Skin = $"{{{{modId}}}}_pokemon_{pokemon.Name}_shiny",
                        Condition = $"selph.ExtraAnimalConfig_ANIMAL_AGE {config.Levelspeed.TextureOverrides}",
                        TextureToUse = $"{config.BasePokemon()}/{pokemon.AlternativeTextureIndex}s"
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
                    Id = $"{{{{modId}}}}_pokemon_extraSpawn_truffle_{pokemon.Name}",
                    ProduceItems = [new ProduceItem {
                        Id = $"{{{{modId}}}}_pokemon_spawn_truffle_{pokemon.Name}",
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
            Name = "{{modId}}_item_egg_bulbasaur",
            DisplayName = "{{i18n:egg.bulbasaur.item}}",
            Description = "{{i18n:egg.bulbasaur.item.description}}",
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

        for (var i = config.Pokemon.Count - 1; i >= 0; i--)
        {
            var spawn = new AnimalSpawnData
            {
                Id = $"{{{{modId}}}}_egg_spawn_{config.Pokemon[i].Name}",
                AnimalId = $"{{{{modId}}}}_pokemon_{config.Pokemon[i].Name}"
            };

            var chance = 1.0 / (i + 1);
            if (chance < 1 )
                spawn.Condition += $"RANDOM {chance:F3}";

            spawnList.Add(spawn);
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
}
