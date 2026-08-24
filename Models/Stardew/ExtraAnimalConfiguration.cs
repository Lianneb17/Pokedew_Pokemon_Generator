namespace PokemonJsonGenerator.Models.Stardew;

public class ExtraAnimalConfigurationChange : EditDataChange<ExtraAnimalConfiguration>
{
    public ExtraAnimalConfigurationChange()
    {
        LogName = "Adding extra texture changes";
        Target = "selph.ExtraAnimalConfig/AnimalExtensionData";
    }
}

public class ExtraAnimalConfiguration : Entry
{
    public List<AppearanceData> TextureOverrides { get; set; } = [];
    public bool? IgnoreRain { get; set; }
    public bool? IgnoreWinter { get; set; }
    public bool? IsHeater { get; set; }
    public string? GlowColor { get; set; }
    public float? GlowRadius { get; set; }
    public List<ExtraProduceSpawnData> ExtraProduceSpawnList { get; set;} = [];
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

public class ExtraProduceSpawnData
{
    public string Id { get; set; } = "";
    public List<ProduceItem> ProduceItems { get; set; } = [];
    public int DaysToProduce { get; set; }
    public bool SyncWithMainProduce { get; set; }
}