using DesktopPets.App.Models;

namespace DesktopPets.App.Services;

public sealed class PetAnimationCycle
{
    private int _nextIndex;

    public string Next()
    {
        var stateName = CodexSpriteSheetState.Options[_nextIndex].StateName;
        _nextIndex = (_nextIndex + 1) % CodexSpriteSheetState.Options.Count;
        return stateName;
    }

    public string NextDifferentFrom(string currentStateName)
    {
        var fallback = Next();
        if (CodexSpriteSheetState.Options.Count <= 1 ||
            !string.Equals(fallback, currentStateName, StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }

        return Next();
    }

    public void Reset()
    {
        _nextIndex = 0;
    }
}
