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

        var config = new Group
        {
            HatchCycle = ToHatchCycle(baseSpecies.HatchCounter),
            Levelspeed = ToLevelspeed(baseSpecies.GrowthRate.Name),
            Color = Datasets.Colors.First(color =>
                string.Equals(color.Color, baseSpecies.Color.Name, StringComparison.OrdinalIgnoreCase)),
            EggGroups = baseSpecies.EggGroups
                .Select(group => ToEggGroup(group.Name))
                .ToList(),
            SpriteWidth = 888,
            SpriteHeight = 999
        };

        config.Pokemon = await BuildPokemonAsync(baseSpecies);
        return config;
    }

    private async Task<List<Pokemon>> BuildPokemonAsync(PokemonSpeciesResponse baseSpecies)
    {
        var evolutionChain = await GetAsync<EvolutionChainResponse>(baseSpecies.EvolutionChain.Url);
        var entries = new List<PokemonEntry>();

        AddEvolutionEntries(evolutionChain.Chain, 1, entries);

        var result = new List<Pokemon>();
        string? secondStageTextureName = null;
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
                    if (genders.Count > 1)
                        entryName += $"_{gender.ToString().ToLowerInvariant()}";

                    if (entry.Stage == 2 && secondStageTextureName is null)
                        secondStageTextureName = entryName;

                    result.Add(new Pokemon
                    {
                        Name = entryName,
                        Gender = gender,
                        BarnType = GetBarnType(entry.Stage),
                        Types = pokemon.Types
                            .OrderBy(type => type.Slot)
                            .Select(type => Enum.Parse<PokemonType>(type.Type.Name, true))
                            .ToList(),
                        CatchRate = ToCatchRate(speciesData.CaptureRate),
                        HasExtraTexture = entry.Stage >= 3,
                        AlternativeTextureName = entry.Stage >= 3 ? secondStageTextureName : null
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

    private async Task<T> GetAsync<T>(string url)
    {
        return await httpClient.GetFromJsonAsync<T>(url)
            ?? throw new InvalidOperationException($"PokeAPI gaf geen data terug voor '{url}'.");
    }

    private static List<Gender> GetGenders(int genderRate, bool hasGenderDifferences)
    {
        if (genderRate == -1)
            return [Gender.Genderless];

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

    private static CatchRateData ToCatchRate(int captureRate)
    {
        return Datasets.CatchRates.First(rate =>
        {
            var bounds = rate.CatchRate.Split('-').Select(int.Parse).ToArray();
            return captureRate <= bounds[0] && captureRate >= bounds[1];
        });
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

        [JsonPropertyName("capture_rate")]
        public int CaptureRate { get; set; }

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
    }

    private sealed class PokemonResponse
    {
        public string Name { get; set; } = "";
        public List<PokemonTypeResponse> Types { get; set; } = [];
        public List<NamedResource> Forms { get; set; } = [];
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