namespace DesktopPets.App.Models;

public static class PetBehaviorMode
{
    public const string Still = "still";
    public const string Patrol = "patrol";
    public const string Loafing = "loafing";

    public static readonly IReadOnlyList<PetBehaviorModeOption> Options =
    [
        new(Still, "静止"),
        new(Patrol, "巡逻"),
        new(Loafing, "摸鱼")
    ];

    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            Still => Still,
            Patrol => Patrol,
            Loafing => Loafing,
            _ => Loafing
        };
    }

    public static string GetDisplayName(string value)
    {
        var normalized = Normalize(value);
        return Options.First(option => option.Mode == normalized).DisplayName;
    }

    public static bool AllowsAutomaticMovement(string value)
    {
        return Normalize(value) != Still;
    }
}

public sealed record PetBehaviorModeOption(string Mode, string DisplayName);
