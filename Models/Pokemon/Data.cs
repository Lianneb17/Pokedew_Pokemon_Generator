namespace PokemonJsonGenerator.Models.Pokemon;

public sealed record HatchCycleData(
    int HatchCycle, int PurchasePrice, int SellPrice, int EggPrice,
    int IncubationTime, int DaysToProduce, int Edibility);

public sealed record LevelspeedData(
    string Levelspeed, int DaysToMature, string TextureOverrides);

public sealed record ColorData(string Color, int SpriteIndex);

public sealed record BaseStatsTotalData(int BaseStatsTotal, int FarmLevel);