using System.IO;
using System.Text.Json;
using DesktopPets.App.Models;

namespace DesktopPets.App.Services;

public sealed class ConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly AppPaths _paths;

    public ConfigurationService(AppPaths paths)
    {
        _paths = paths;
    }

    public AppConfiguration LoadOrCreate()
    {
        _paths.EnsureBaseDirectories();

        if (!File.Exists(_paths.ConfigPath))
        {
            var created = AppConfiguration.CreateDefault();
            Save(created);
            return created;
        }

        try
        {
            var json = File.ReadAllText(_paths.ConfigPath);
            var configuration = JsonSerializer.Deserialize<AppConfiguration>(json, JsonOptions);
            if (configuration is null || configuration.Version <= 0)
            {
                return ResetToDefault();
            }

            configuration.Scale = PetScaleSettings.Normalize(configuration.Scale);

            if (configuration.AutoWalkRange <= 0)
            {
                configuration.AutoWalkRange = 180;
            }

            NormalizeBehaviorMode(configuration, HasBehaviorMode(json));
            return configuration;
        }
        catch (JsonException)
        {
            return ResetToDefault();
        }
        catch (IOException)
        {
            return AppConfiguration.CreateDefault();
        }
        catch (UnauthorizedAccessException)
        {
            return AppConfiguration.CreateDefault();
        }
    }

    public void Save(AppConfiguration configuration)
    {
        _paths.EnsureBaseDirectories();
        configuration.Version = AppConfiguration.CurrentVersion;
        NormalizeBehaviorMode(configuration, hasBehaviorMode: true);

        var json = JsonSerializer.Serialize(configuration, JsonOptions);
        File.WriteAllText(_paths.ConfigPath, json);
    }

    private AppConfiguration ResetToDefault()
    {
        var configuration = AppConfiguration.CreateDefault();
        Save(configuration);
        return configuration;
    }

    private static void NormalizeBehaviorMode(AppConfiguration configuration, bool hasBehaviorMode)
    {
        if (!hasBehaviorMode || string.IsNullOrWhiteSpace(configuration.BehaviorMode))
        {
            configuration.BehaviorMode = configuration.AutoWalkEnabled
                ? PetBehaviorMode.Loafing
                : PetBehaviorMode.Still;
        }
        else
        {
            configuration.BehaviorMode = PetBehaviorMode.Normalize(configuration.BehaviorMode);
        }

        configuration.AutoWalkEnabled = PetBehaviorMode.AllowsAutomaticMovement(configuration.BehaviorMode);
    }

    private static bool HasBehaviorMode(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("behaviorMode", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
