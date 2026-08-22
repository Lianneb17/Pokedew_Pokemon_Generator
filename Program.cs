namespace PokemonJsonGenerator;

internal static class Program
{
    public static void Main()
    {
        var config = Questions.AskGeneratorConfig();

        var outputPath = Path.Combine(
            Environment.CurrentDirectory,
            "modId_generated.json");

        try
        {
            JsonGenerator.Write(config, outputPath);

            Console.WriteLine();
            Console.WriteLine("=== Klaar ===");
            Console.WriteLine($"JSON geschreven naar:");
            Console.WriteLine(outputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Genereren mislukt: {ex.Message}");
        }
    }
}
