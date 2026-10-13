namespace DesktopPets.App.Models;

public sealed class AppConfiguration
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public double Scale { get; set; } = PetScaleSettings.Default;

    public string? CurrentPetManifest { get; set; }

    public bool AutoWalkEnabled { get; set; } = true;

    public double AutoWalkRange { get; set; } = 180;

    public string BehaviorMode { get; set; } = PetBehaviorMode.Loafing;

    public static AppConfiguration CreateDefault()
    {
        return new AppConfiguration
        {
            Version = CurrentVersion,
            Scale = PetScaleSettings.Default,
            AutoWalkEnabled = true,
            AutoWalkRange = 180,
            BehaviorMode = PetBehaviorMode.Loafing
        };
    }
}
