namespace PokemonJsonGenerator.Models;

public sealed class GeneratedJson
{
    public List<Change> Changes { get; set; } = [];
}

public abstract class Change
{
    public string LogName { get; set; } = "";
    public abstract string Action { get; }
    public string Target { get; set; } = "";

}

public sealed class LoadChange : Change
{
    public override string Action => "Load";
    public string FromFile { get; set; } = "";
}

public class EditDataChange<T> : Change where T : Entry
{
    public override string Action => "EditData";
    public Dictionary<string, T> Entries { get; set; } = [];
}

public abstract class Entry
{
    public string ID {get; set;} = "";
}

public sealed class Sound : Entry
{    
    public string Category { get; set; } = "Sound";
    public List<string> FilePaths {get; set;} = [];
}

public sealed class SoundChange : EditDataChange<Sound>
{
    public SoundChange()
    {
        LogName = "Adding sound changes";
        Target = "Data/AudioChanges";
    }
}

public sealed class AnimalChange : EditDataChange<Animal>
{
    public AnimalChange()
    {
        LogName = "Adding animal changes";
        Target = "Data/AnimalChanges";
    }
}

public class Animal : Entry
{
    // main info
    public string DisplayName { get; set; } = "";
    public BarnType House { get; set; }
    // default female
    public Gender Gender { get; set; }

    // Animal shop
    // default -1
    public int? PurchasePrice { get; set; }
    public string? ShopTexture { get; set; }
    public string? ShopTextureSourceRect { get; set; }
    // default none
    public BarnType? RequiredBuilding { get; set; }
    // default unlocked
    public string? UnlockCondition { get; set; }
    // defaultsto DisplayName
    public string? ShopDisplayName { get; set; }
    // default none
    public string? ShopDescription { get; set; }
    // default none
    public string? ShopMissingBuildingDescription { get; set; }
    // default none
    public List<AlternatePurchaseType> AlternatePurchaseTypes { get; set; } = [];
    
    // Hatching
    // default none
    public List<string> EggItemIds { get; set; } = [];
    // default 9000
    public int? IncubationTime { get; set; }
    // default 1
    public int? IncubatorParentSheetOffset { get; set; }
    // default "???"
    public string? BirthText { get; set; }

    // Growth
    // default 1
    public int? DaysToMature { get; set; }
    // default false
    public bool? CanGetPregnant { get; set; }

    // Produce
    public List<ProduceItem> ProduceItemIds { get; set; } = [];
    public List<ProduceItem> DeluxeProduceItemIds { get; set; } = [];
    // default 1
    public int? DaysToProduce { get; set; }
    // default false
    public bool? ProduceOnMature { get; set; }
    // default no reduction
    public bool? FriendshipForFasterProduce { get; set; }
    // default 200
    public int? DeluxeProduceMinimumFriendship { get; set; }
    // default 1200
    public int? DeluxeProduceCareDivisor { get; set; }
    // default 0
    public int? DeluxeProduceLuckMultiplier { get; set; }
    // 
    public HarvestType? HarvestType { get; set; }
    // default none
    public string? HarvestTool { get; set; }
    // default true
    public bool? CanEatGoldenCrackers { get; set; }

    // Audio & Sprite
    // default none
    public string? Sound { get; set; }
    // default none
    public string? BabySound { get; set; }
    // default "Animals/<ID>"
    public string? Texture { get; set; }
    // default none
    public string? HarvestedTexture { get; set; }
    // default none
    public string? BabyTexture { get; set; }
    // default false
    public bool? UseFlippedRightForLeft { get; set; }
    // default 16
    public int? SpriteWidth { get; set; }
    // default 16
    public int? SpriteHeight { get; set; }
    // default zero
    public int? EmoteOffset { get; set; }
    // default "X": 0, "Y": 112
    public string? SwimOffset { get; set; }
    public List<Skin> Skins { get; set; } = [];
    // default 12
    public int? SleepFrame { get; set; }
    // default false
    public bool? UseDoubleUniqueAnimationFrames { get; set; }
    // Shadows not implemented at the moment
    public string? ShadowWhenBaby { get; set; }
    public string? ShadowWhenBabySwims { get; set; }
    public string? ShadowWhenAdult { get; set; }
    public string? ShadowWhenAdultSwims { get; set; }
    public string? Shadow { get; set; }

    // Player profession effects
    // default none
    public string? ProfessionForFasterProduce { get; set; }
    // default none
    public string? ProfessionForHappinessBoost { get; set; }
    // default none
    public string? ProfessionForQualityBoost { get; set; }

    // Behavior
    // default false
    public bool? CanSwim { get; set; }
    // default false
    public bool? BabiesFollowAdults { get; set; }
    // default 2
    public int? GrassEatAmount { get; set; }
    // default 0
    public int? HappinessDrain { get; set; }
    // default 0
    public int? SellPrice { get; set; }
    public string? CustomFields { get; set; }

    // Other
    // default false
    public bool? ShowInSummitCredits { get; set; }
    // Not implemented yet
    public string? StatToIncrementOnProduce { get; set; }
    // default 1x1
    public string? UpDownPetHitboxTileSize { get; set; }
    // default 1x1
    public string? LeftRightPetHitboxTileSize { get; set; }
    // default 1x1
    public string? BabyUpDownPetHitboxTileSize { get; set; }
    // default 1x1
    public string? BabyLeftRightPetHitboxTileSize { get; set; }
}

public class AlternatePurchaseType : Entry
{
    public List<string> AnimalIDs { get; set; } = [];
    public string? Condition { get; set; }
}

public class ProduceItem
{
    public string Id {get; set; } = "";
    public string ItemID { get; set; } = "";
    // Default always true
    public string? Condition { get; set; }
    // Default 1
    public int? MinimumFriendship { get; set; }
}

public enum HarvestType
{
    DropOvernight, HarvestWithTool, DigUp
}

public class Skin
{
    public string ID { get; set; } = "";
    //default 1.0
    public double? Weight { get; set; }
    // default main field
    public string? Texture { get; set; }
    // default main field
    public string? HarvestedTexture { get; set; }
    // default main field
    public string? BabyTexture { get; set; }
}

public class ExtraTextureChange : EditDataChange<ExtraTexture>
{
    public ExtraTextureChange()
    {
        LogName = "Adding extra texture changes";
        Target = "selph.ExtraAnimalConfig/AnimalExtensionData";
    }
}

public class ExtraTexture : Entry
{
    public List<AppearanceData> TextureOverrides { get; set; } = [];
}

public class AppearanceData 
{
    public string Id { get; set; } = "";
    public string? Produce { get; set; }
    public string? Skin { get; set; }
    public string? Condition { get; set; }
    public string? TextureToUse { get; set; }
    public DefaultTexture? DefaultTextureToUse { get; set; }
}

public enum DefaultTexture
{
    Texture, HarvestedTexture, BabyTexture
}

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
    public string? Buffs { get; set; }

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

public enum ObjectType
{
    Basic, Arch, Litter, Minerals, Quest, Crafting, Fish, Cooking, Seeds, Ring, interactive, asdf
}

public class EggExtensionChange : EditDataChange<EggExtension>
{
    public EggExtensionChange()
    {
        LogName = "Adding egg extension changes";
        Target = "selph.ExtraAnimalConfig/EggExtensionData";
    }
}

public class EggExtension : Entry
{
    public List<AnimalSpawnData> AnimalSpawnList { get; set; } = [];
}

public class AnimalSpawnData
{
    public string Id { get; set; } = "";
    public string AnimalId { get; set; } = "";
    public string? Condition { get; set; }
}