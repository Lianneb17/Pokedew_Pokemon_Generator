using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PokemonJsonGenerator.Models.Pokemon;

namespace PokemonJsonGenerator;

public sealed class PokeApiClient
{
    private const string ApiBaseUrl = "https://pokeapi.co/api/v2/";
    private readonly HttpClient httpClient = new() { BaseAddress = new Uri(ApiBaseUrl) };

    public async Task<Group> BuildGroupAsync(string name)
    {
        var baseSpecies = await GetAsync<PokemonSpeciesResponse>($"pokemon-species/{name}");
        var evolutionChain = await GetAsync<EvolutionChainResponse>(baseSpecies.EvolutionChain.Url);

        var config = new Group
        {
            BasePokemonName = evolutionChain.Chain.Species.Name.Replace('-', '_'),
            HatchCycle = ToHatchCycle(baseSpecies.HatchCounter),
            Levelspeed = ToLevelspeed(baseSpecies.GrowthRate.Name),
            Color = Datasets.Colors.First(color =>
                string.Equals(color.Color, baseSpecies.Color.Name, StringComparison.OrdinalIgnoreCase)),
            EggGroups = [.. baseSpecies.EggGroups.Select(group => ToEggGroup(group.Name))],
            SpriteWidth = 29,
            SpriteHeight = 21,
            Evolutions = BuildEvolutions(evolutionChain.Chain),
            Pokemon = await BuildPokemonAsync(baseSpecies, evolutionChain)
        };
        return config;
    }

    private async Task<List<Pokemon>> BuildPokemonAsync(
        PokemonSpeciesResponse baseSpecies,
        EvolutionChainResponse evolutionChain)
    {
        var entries = new List<PokemonEntry>();

        AddEvolutionEntries(evolutionChain.Chain, 1, entries);

        var result = new List<Pokemon>();
        foreach (var entry in entries)
        {
            var speciesData = await GetAsync<PokemonSpeciesResponse>(entry.SpeciesUrl);
            var genders = GetGenders(speciesData.GenderRate, speciesData.HasGenderDifferences);

            foreach (var variety in speciesData.Varieties)
            {
                var pokemon = await GetAsync<PokemonResponse>(variety.Pokemon.Url);
                if (await IsMegaAsync(pokemon))
                    continue;

                foreach (var gender in genders)
                {
                    var entryName = pokemon.Name.Replace('-', '_');

                    result.Add(new Pokemon
                    {
                        Name = entryName,
                        Gender = gender,
                        HasGenderVariants = genders.Count > 1,
                        BarnType = GetBarnType(entry.Stage),
                        Types = [.. pokemon.Types
                            .OrderBy(type => type.Slot)
                            .Select(type => Enum.Parse<PokemonType>(type.Type.Name, true))],
                        BaseStatsTotal = ToBaseStatsTotal(pokemon.Stats.Sum(stat => stat.BaseStat))
                    });
                }
            }
        }

        return result;
    }

    private async Task<bool> IsMegaAsync(PokemonResponse pokemon)
    {
        foreach (var form in pokemon.Forms)
        {
            var formData = await GetAsync<PokemonFormResponse>(form.Url);
            if (formData.IsMega || formData.IsBattleOnly)
                return true;
        }

        return false;
    }

    private static HatchCycleData ToHatchCycle(int hatchCounter)
    {
        return Datasets.HatchCycles
            .OrderBy(cycle => Math.Abs(cycle.HatchCycle - hatchCounter))
            .First();
    }

    private static LevelspeedData ToLevelspeed(string growthRate)
    {
        var normalizedName = growthRate.Replace('-', ' ');
        var mappedName = normalizedName switch
        {
            "medium slow" => "slighty slow",
            "medium" => "medium slow",
            _ => normalizedName
        };

        return Datasets.Levelspeeds.FirstOrDefault(levelspeed =>
            string.Equals(levelspeed.Levelspeed, mappedName, StringComparison.OrdinalIgnoreCase))
            ?? Datasets.Levelspeeds.First(levelspeed => levelspeed.Levelspeed == "medium fast");
    }

    private static EggGroup ToEggGroup(string name)
    {
        var normalizedName = name.Replace("-", "", StringComparison.OrdinalIgnoreCase);
        return normalizedName switch
        {
            "monster" => EggGroup.Monster,
            "water1" => EggGroup.Water1,
            "bug" => EggGroup.Bug,
            "flying" => EggGroup.Flying,
            "field" => EggGroup.Field,
            "ground" => EggGroup.Field,
            "fairy" => EggGroup.Fairy,
            "plant" => EggGroup.Grass,
            "grass" => EggGroup.Grass,
            "humanlike" => EggGroup.HumanLike,
            "humanshape" => EggGroup.HumanLike,
            "water3" => EggGroup.Water3,
            "mineral" => EggGroup.Mineral,
            "amorphous" => EggGroup.Amorphous,
            "water2" => EggGroup.Water2,
            "ditto" => EggGroup.Ditto,
            "dragon" => EggGroup.Dragon,
            "noeggs" or "undiscovered" => EggGroup.NoEggDiscovered,
            "genderunknown" => EggGroup.GenderUnknown,
            "indeterminate" => EggGroup.Amorphous,
            _ => throw new InvalidOperationException($"Onbekende Egg Group '{name}'.")
        };
    }

    private void AddEvolutionEntries(
        EvolutionChainLink link,
        int stage,
        ICollection<PokemonEntry> entries)
    {
        entries.Add(new PokemonEntry(link.Species.Url, stage));

        foreach (var next in link.EvolvesTo)
            AddEvolutionEntries(next, stage + 1, entries);
    }

    private static List<Evolution> BuildEvolutions(EvolutionChainLink root)
    {
        var evolutions = new List<Evolution>();
        AddEvolutions(root, 2, evolutions);
        return evolutions;
    }

    private static void AddEvolutions(
        EvolutionChainLink link,
        int stage,
        ICollection<Evolution> evolutions)
    {
        var from = link.Species.Name.Replace('-', '_');
        foreach (var next in link.EvolvesTo)
        {
            foreach (var detail in next.EvolutionDetails)
            {
                evolutions.Add(new Evolution
                {
                    From = from,
                    To = next.Species.Name.Replace('-', '_'),
                    Stage = stage,
                    Trigger = detail.Trigger?.Name,
                    MinimumLevel = detail.MinimumLevel,
                    Item = detail.Item?.Name
                });
            }

            AddEvolutions(next, stage + 1, evolutions);
        }
    }

    private async Task<T> GetAsync<T>(string url)
    {
        return await httpClient.GetFromJsonAsync<T>(url)
            ?? throw new InvalidOperationException($"PokeAPI gaf geen data terug voor '{url}'.");
    }

    private static List<Gender> GetGenders(int genderRate, bool hasGenderDifferences)
    {
        // Genderless = both
        if (genderRate == -1)
            return [Gender.MaleOrFemale];

        if (hasGenderDifferences)
            return [Gender.Male, Gender.Female];

        return genderRate switch
        {
            0 => [Gender.Male],
            8 => [Gender.Female],
            _ => [Gender.MaleOrFemale]
        };
    }

    private static BarnType GetBarnType(int stage) => stage switch
    {
        1 => BarnType.PokeBarn,
        2 => BarnType.BigPokeBarn,
        _ => BarnType.DeluxePokeBarn
    };

    private static BaseStatsTotalData ToBaseStatsTotal(int baseStatsTotal)
    {
        var farmLevel = baseStatsTotal switch
        {
            <= 300 => 0,
            <= 330 => 1,
            <= 360 => 2,
            <= 390 => 3,
            <= 420 => 4,
            <= 450 => 5,
            <= 480 => 6,
            <= 510 => 7,
            <= 540 => 8,
            <= 580 => 9,
            _ => 10
        };

        return new BaseStatsTotalData(baseStatsTotal, farmLevel);
    }

    private sealed record PokemonEntry(
        string SpeciesUrl,
        int Stage);

    private sealed record NamedResource(
        string Name,
        string Url);

    private sealed class PokemonSpeciesResponse
    {
        [JsonPropertyName("gender_rate")]
        public int GenderRate { get; set; }

        [JsonPropertyName("hatch_counter")]
        public int HatchCounter { get; set; }

        [JsonPropertyName("growth_rate")]
        public NamedResource GrowthRate { get; set; } = null!;

        public NamedResource Color { get; set; } = null!;

        [JsonPropertyName("egg_groups")]
        public List<NamedResource> EggGroups { get; set; } = [];

        [JsonPropertyName("has_gender_differences")]
        public bool HasGenderDifferences { get; set; }

        public List<PokemonVarietyResponse> Varieties { get; set; } = [];

        [JsonPropertyName("evolution_chain")]
        public NamedResource EvolutionChain { get; set; } = null!;
    }

    private sealed class PokemonVarietyResponse
    {
        public NamedResource Pokemon { get; set; } = null!;
    }

    private sealed class EvolutionChainResponse
    {
        public EvolutionChainLink Chain { get; set; } = null!;
    }

    private sealed class EvolutionChainLink
    {
        public NamedResource Species { get; set; } = null!;

        [JsonPropertyName("evolves_to")]
        public List<EvolutionChainLink> EvolvesTo { get; set; } = [];

        [JsonPropertyName("evolution_details")]
        public List<EvolutionDetails> EvolutionDetails { get; set; } = [];
    }

    private sealed class EvolutionDetails
    {
        public NamedResource? Item { get; set; }
        public NamedResource? Trigger { get; set; }

        [JsonPropertyName("min_level")]
        public int? MinimumLevel { get; set; }
    }

    private sealed class PokemonResponse
    {
        public string Name { get; set; } = "";
        public List<PokemonTypeResponse> Types { get; set; } = [];
        public List<PokemonStatResponse> Stats { get; set; } = [];
        public List<NamedResource> Forms { get; set; } = [];
    }

    private sealed class PokemonStatResponse
    {
        [JsonPropertyName("base_stat")]
        public int BaseStat { get; set; }
    }

    private sealed class PokemonFormResponse
    {
        [JsonPropertyName("is_mega")]
        public bool IsMega { get; set; }

        [JsonPropertyName("is_battle_only")]
        public bool IsBattleOnly { get; set; }
    }

    private sealed class PokemonTypeResponse
    {
        public int Slot { get; set; }
        public NamedResource Type { get; set; } = null!;
    }
}