using System.IO;

namespace DesktopPets.App.Services;

public sealed class AppPaths
{
    public AppPaths(string? rootDirectory = null)
    {
        RootDirectory = rootDirectory ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopPets");
    }

    public string RootDirectory { get; }

    public string ConfigPath => Path.Combine(RootDirectory, "config.json");

    public string PetsDirectory => Path.Combine(RootDirectory, "pets");

    public string GeneratedPetsDirectory => Path.Combine(PetsDirectory, "generated");

    public void EnsureBaseDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(GeneratedPetsDirectory);
    }
}
