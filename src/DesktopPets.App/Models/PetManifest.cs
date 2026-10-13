namespace DesktopPets.App.Models;

public sealed class PetManifest
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    public string SourceImage { get; set; } = string.Empty;

    public Dictionary<string, PetStateManifest> States { get; set; } = new();
}
