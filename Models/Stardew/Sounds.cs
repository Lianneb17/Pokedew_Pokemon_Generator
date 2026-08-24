namespace PokemonJsonGenerator.Models.Stardew;

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