namespace PokemonJsonGenerator.Models.Stardew;

public class ObjectChange : EditDataChange<ObjectData>
{
    public ObjectChange()
    {
        LogName = "Adding object changes";
        Target = "Data/ObjectChanges";
    }
}

public class ObjectData : Entry
{
    // Basic info
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public ObjectType Type { get; set; }
    public int Category { get; set; }
    // Default 0
    public int? Price { get; set; }

    // Appearance
    // Default Maps/springobjects
    public string? Texture { get; set; }
    public int SpriteIndex { get; set; }
    // Default false
    public bool? ColorOverlayFromNextIndex { get; set; }

    // Edibility
    // Default -300
    public int? Edibility { get; set; }
    // default false
    public bool? IsDrink { get; set; }
    // Not implemented yet
    public List<Buff> Buffs { get; set; }

    // Geode & artifact spots
    // Not implemented yet
    public string? GeodeDrops { get; set; }
    // Not implemented yet
    public string? GeodeDropsDefaultItems { get; set; }
    public double? ArtifactSpotChances { get; set; }

    // Context tags & exclusions
    public List<string> ContextTags { get; set; } = [];
    // default true
    public bool? CanBeGivenAsGift { get; set; }
    // default true
    public bool? CanBeTrashed { get; set; }
    // default false
    public bool? ExcludeFromRandomSale { get; set; }
    // default false
    public bool? ExcludeFromFishingCollection { get; set; }
    // default false
    public bool? ExcludeFromShippingCollection { get; set; }

    // Advanced
    public string? CustomFields { get; set; }
}

public class Buff {
    public string Id { get; set; } = "";
    public int Duration { get; set; }
    public string? IconTexture { get; set; }
    public string? IconSpriteIndex { get; set; }
    public Effects? Effects { get; set; }
}

public class Effects
{
    public int? Attack { get; set; }
}