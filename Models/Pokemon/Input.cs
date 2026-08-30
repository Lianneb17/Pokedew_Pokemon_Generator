namespace PokemonJsonGenerator.Models.Pokemon;

public sealed class Pokemon
{
    public string Name { get; set; } = "";
    public Gender Gender { get; set; }
    public BarnType BarnType { get; set; }
    public List<PokemonType> Types { get; set; } = [];
    public BaseStatsTotalData BaseStatsTotal { get; set; } = null!;
    public bool HasExtraTexture { get; set; }
    public string? AlternativeTextureName { get; set; }
}

public sealed class Group
{
    public HatchCycleData HatchCycle { get; set; } = null!;
    public LevelspeedData Levelspeed { get; set; } = null!;
    public ColorData Color { get; set; } = null!;
    public List<EggGroup> EggGroups { get; set; } = [];
    public int SpriteWidth { get; set; }
    public int SpriteHeight { get; set; }
    public bool ShouldBeForSale { get; set; }
    public List<Pokemon> Pokemon { get; set; } = [];
}