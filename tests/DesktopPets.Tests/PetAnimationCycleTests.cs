using DesktopPets.App.Models;
using DesktopPets.App.Services;

namespace DesktopPets.Tests;

public sealed class PetAnimationCycleTests
{
    [Fact]
    public void NextCyclesThroughAllCodexStates()
    {
        var cycle = new PetAnimationCycle();

        var firstPass = Enumerable.Range(0, CodexSpriteSheetState.Options.Count)
            .Select(_ => cycle.Next())
            .ToList();
        var next = cycle.Next();

        Assert.Equal(CodexSpriteSheetState.Options.Select(option => option.StateName), firstPass);
        Assert.Equal(CodexSpriteSheetState.Options[0].StateName, next);
    }

    [Fact]
    public void NextDifferentFromSkipsCurrentState()
    {
        var cycle = new PetAnimationCycle();

        var next = cycle.NextDifferentFrom("idle");

        Assert.Equal("running-right", next);
    }

    [Fact]
    public void NextDifferentFromKeepsCycleOrderWhenCurrentStateIsDifferent()
    {
        var cycle = new PetAnimationCycle();

        var next = cycle.NextDifferentFrom("waving");

        Assert.Equal("idle", next);
    }
}
