namespace PokemonJsonGenerator.Models.Pokemon;

public sealed class PokemonConfig
{
    public string Name { get; set; } = "";
    public Gender Gender { get; set; }
    public BarnType BarnType { get; set; }
    public List<PokemonType> Types { get; set; } = [];
    public List<EggGroup> Groups { get; set; } = [];
    public bool HasExtraTexture { get; set; }
    public int? AlternativeTextureIndex { get; set; }
}

public sealed class GeneratorConfig
{
    public HatchCycleData HatchCycle { get; set; } = null!;
    public LevelspeedData Levelspeed { get; set; } = null!;
    public ColorData Color { get; set; } = null!;
    public List<EggGroup> Groups { get; set; } = [];
    public int SpriteWidth { get; set; }
    public int SpriteHeight { get; set; }
    public List<PokemonConfig> Pokemon { get; set; } = [];
}