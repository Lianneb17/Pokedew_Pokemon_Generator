namespace PokemonJsonGenerator.Models.Stardew;

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