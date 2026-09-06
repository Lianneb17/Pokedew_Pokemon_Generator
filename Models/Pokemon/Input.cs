namespace PokemonJsonGenerator.Models.Pokemon;

public sealed class Pokemon
{
    public string Name { get; set; } = "";
    public Gender Gender { get; set; }
    public bool HasGenderVariants { get; set; }
    public BarnType BarnType { get; set; }
    public List<PokemonType> Types { get; set; } = [];
    public BaseStatsTotalData BaseStatsTotal { get; set; } = null!;
}

public sealed class Group
{
    public string BasePokemonName { get; set; } = "";
    public HatchCycleData HatchCycle { get; set; } = null!;
    public LevelspeedData Levelspeed { get; set; } = null!;
    public ColorData Color { get; set; } = null!;
    public List<EggGroup> EggGroups { get; set; } = [];
    public int SpriteWidth { get; set; }
    public int SpriteHeight { get; set; }
    public bool ShouldBeForSale { get; set; }
    public List<Evolution> Evolutions { get; set; } = [];
    public List<Pokemon> Pokemon { get; set; } = [];
}

public sealed class Evolution
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public int Stage { get; set; }
    public string? Trigger { get; set; }
    public int? MinimumLevel { get; set; }
    public string? Item { get; set; }
}