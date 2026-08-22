namespace PokemonJsonGenerator;

public enum Gender { Male, Female, MaleOrFemale }

public enum BarnType { PokeBarn, BigPokeBarn, DeluxePokeBarn }

public enum EggGroup
{
    Monster, Water1, Bug, Flying, Field, Fairy, Grass, HumanLike,
    Water3, Mineral, Amorphous, Water2, Ditto, Dragon,
    NoEggDiscovered, GenderUnknown
}

public sealed record HatchCycleData(
    int HatchCycle, int PurchasePrice, int SellPrice, int EggPrice,
    int IncubationTime, int DaysToProduce, int Edibility);

public sealed record LevelspeedData(
    string Levelspeed, int DaysToMature, string TextureOverrides);

public sealed record ColorData(string Color, int SpriteIndex);

public sealed class PokemonConfig
{
    public string Name { get; set; } = "";
    public Gender Gender { get; set; }
    public BarnType BarnType { get; set; }
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
