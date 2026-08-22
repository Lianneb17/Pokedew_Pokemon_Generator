using PokemonJsonGenerator.Models;

namespace PokemonJsonGenerator;

public static class Datasets
{
    public static readonly HatchCycleData[] HatchCycles =
    [
        new(5, 500, 1000, 100, 9000, 1, 10),
        new(10, 1500, 3000, 300, 18000, 2, 30),
        new(20, 2500, 5000, 500, 27000, 3, 50),
        new(30, 3500, 7000, 700, 36000, 4, 700),
        new(40, 5500, 11000, 1100, 45000, 7, 1100)
    ];

    public static readonly LevelspeedData[] Levelspeeds =
    [
        new("slow", 10, "6 10"),
        new("slighty slow", 8, "5 8"),
        new("medium slow", 7, "4 7"),
        new("medium fast", 6, "4 6"),
        new("slightly fast", 5, "3 5"),
        new("fast", 3, "2 3")
    ];

    public static readonly ColorData[] Colors =
    [
        new("red", 0), new("blue", 1), new("yellow", 2),
        new("green", 3), new("black", 4), new("brown", 5),
        new("purple", 6), new("gray", 7), new("white", 8),
        new("pink", 9)
    ];

    public static readonly EggGroup[] EggGroups =
    [
        EggGroup.Monster, EggGroup.Water1, EggGroup.Bug, EggGroup.Flying,
        EggGroup.Field, EggGroup.Fairy, EggGroup.Grass, EggGroup.HumanLike,
        EggGroup.Water3, EggGroup.Mineral, EggGroup.Amorphous, EggGroup.Water2,
        EggGroup.Ditto, EggGroup.Dragon, EggGroup.NoEggDiscovered,
        EggGroup.GenderUnknown
    ];
}
