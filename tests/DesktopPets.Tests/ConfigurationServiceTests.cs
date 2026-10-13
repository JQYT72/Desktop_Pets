using System.Text.Json;
using DesktopPets.App.Models;
using DesktopPets.App.Services;

namespace DesktopPets.Tests;

public sealed class ConfigurationServiceTests
{
    [Fact]
    public void LoadOrCreateCreatesDefaultConfigurationWhenMissing()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        var service = new ConfigurationService(paths);

        var configuration = service.LoadOrCreate();

        Assert.Equal(AppConfiguration.CurrentVersion, configuration.Version);
        Assert.Equal(1.0, configuration.Scale);
        Assert.True(configuration.AutoWalkEnabled);
        Assert.Equal(180, configuration.AutoWalkRange);
        Assert.Equal(PetBehaviorMode.Loafing, configuration.BehaviorMode);
        Assert.True(File.Exists(paths.ConfigPath));
    }

    [Fact]
    public void LoadOrCreateResetsDamagedConfiguration()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        File.WriteAllText(paths.ConfigPath, "{ not-json");

        var configuration = new ConfigurationService(paths).LoadOrCreate();

        Assert.Equal(AppConfiguration.CurrentVersion, configuration.Version);
        Assert.Null(configuration.CurrentPetManifest);

        var saved = JsonSerializer.Deserialize<AppConfiguration>(
            File.ReadAllText(paths.ConfigPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(saved);
        Assert.Equal(AppConfiguration.CurrentVersion, saved!.Version);
    }

    [Fact]
    public void LoadOrCreateResetsInvalidScale()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        File.WriteAllText(
            paths.ConfigPath,
            """
            {
              "version": 1,
              "scale": 8.0,
              "behaviorMode": "loafing"
            }
            """);

        var configuration = new ConfigurationService(paths).LoadOrCreate();

        Assert.Equal(PetScaleSettings.Default, configuration.Scale);
    }

    [Fact]
    public void LoadOrCreateAddsAutoWalkDefaultsForOldConfiguration()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        File.WriteAllText(
            paths.ConfigPath,
            """
            {
              "version": 1,
              "scale": 1.0,
              "currentPetManifest": null
            }
            """);

        var configuration = new ConfigurationService(paths).LoadOrCreate();

        Assert.True(configuration.AutoWalkEnabled);
        Assert.Equal(180, configuration.AutoWalkRange);
        Assert.Equal(PetBehaviorMode.Loafing, configuration.BehaviorMode);
    }

    [Fact]
    public void LoadOrCreateMapsDisabledAutoWalkToStillModeForOldConfiguration()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        File.WriteAllText(
            paths.ConfigPath,
            """
            {
              "version": 1,
              "scale": 1.0,
              "autoWalkEnabled": false,
              "autoWalkRange": 180
            }
            """);

        var configuration = new ConfigurationService(paths).LoadOrCreate();

        Assert.False(configuration.AutoWalkEnabled);
        Assert.Equal(PetBehaviorMode.Still, configuration.BehaviorMode);
    }

    [Fact]
    public void SaveKeepsAutoWalkCompatibilityInSyncWithBehaviorMode()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        var service = new ConfigurationService(paths);
        var configuration = AppConfiguration.CreateDefault();
        configuration.BehaviorMode = PetBehaviorMode.Patrol;
        configuration.AutoWalkEnabled = false;

        service.Save(configuration);

        var saved = JsonSerializer.Deserialize<AppConfiguration>(
            File.ReadAllText(paths.ConfigPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(saved);
        Assert.Equal(PetBehaviorMode.Patrol, saved!.BehaviorMode);
        Assert.True(saved.AutoWalkEnabled);
    }
}
