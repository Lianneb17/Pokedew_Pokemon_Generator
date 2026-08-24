namespace PokemonJsonGenerator.Models.Stardew;

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