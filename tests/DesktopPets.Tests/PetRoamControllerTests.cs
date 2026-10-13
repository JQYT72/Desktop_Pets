using DesktopPets.App.Models;
using DesktopPets.App.Services;

namespace DesktopPets.Tests;

public sealed class PetRoamControllerTests
{
    [Fact]
    public void CalculateNextStepDoesNotMoveWhenDisabled()
    {
        var result = PetRoamController.CalculateNextStep(
            currentLeft: 100,
            windowWidth: 50,
            centerLeft: 100,
            range: 180,
            minLeft: 0,
            maxRight: 500,
            direction: 1,
            stepSize: 2,
            enabled: false);

        Assert.Equal(100, result.NextLeft);
        Assert.False(result.Moved);
    }

    [Fact]
    public void CalculateNextStepClampsAtRightBoundaryAndReverses()
    {
        var result = PetRoamController.CalculateNextStep(
            currentLeft: 119,
            windowWidth: 50,
            centerLeft: 100,
            range: 20,
            minLeft: 0,
            maxRight: 500,
            direction: 1,
            stepSize: 5,
            enabled: true);

        Assert.Equal(120, result.NextLeft);
        Assert.Equal(-1, result.Direction);
        Assert.True(result.Moved);
        Assert.True(result.HitBoundary);
    }

    [Fact]
    public void CalculateNextStepClampsAtScreenBoundary()
    {
        var result = PetRoamController.CalculateNextStep(
            currentLeft: 445,
            windowWidth: 60,
            centerLeft: 470,
            range: 180,
            minLeft: 0,
            maxRight: 500,
            direction: 1,
            stepSize: 10,
            enabled: true);

        Assert.Equal(440, result.NextLeft);
        Assert.Equal(-1, result.Direction);
        Assert.True(result.HitBoundary);
    }

    [Fact]
    public void CalculateNextStepForBehaviorDoesNotMoveInStillMode()
    {
        var result = PetRoamController.CalculateNextStepForBehavior(
            PetBehaviorMode.Still,
            loafingMoveActive: false,
            extendedPatrolActive: false,
            currentLeft: 100,
            windowWidth: 50,
            centerLeft: 100,
            baseRange: 180,
            minLeft: 0,
            maxRight: 500,
            direction: 1,
            stepSize: 2,
            enabled: true);

        Assert.Equal(100, result.NextLeft);
        Assert.False(result.Moved);
    }

    [Fact]
    public void CalculateNextStepForBehaviorMovesInPatrolMode()
    {
        var result = PetRoamController.CalculateNextStepForBehavior(
            PetBehaviorMode.Patrol,
            loafingMoveActive: false,
            extendedPatrolActive: false,
            currentLeft: 100,
            windowWidth: 50,
            centerLeft: 100,
            baseRange: 180,
            minLeft: 0,
            maxRight: 500,
            direction: 1,
            stepSize: 2,
            enabled: true);

        Assert.Equal(102, result.NextLeft);
        Assert.True(result.Moved);
    }

    [Fact]
    public void CalculateNextStepForBehaviorMovesInLoafingOnlyWhenActive()
    {
        var idle = PetRoamController.CalculateNextStepForBehavior(
            PetBehaviorMode.Loafing,
            loafingMoveActive: false,
            extendedPatrolActive: false,
            currentLeft: 100,
            windowWidth: 50,
            centerLeft: 100,
            baseRange: 180,
            minLeft: 0,
            maxRight: 500,
            direction: 1,
            stepSize: 2,
            enabled: true);
        var active = PetRoamController.CalculateNextStepForBehavior(
            PetBehaviorMode.Loafing,
            loafingMoveActive: true,
            extendedPatrolActive: false,
            currentLeft: 100,
            windowWidth: 50,
            centerLeft: 100,
            baseRange: 180,
            minLeft: 0,
            maxRight: 500,
            direction: 1,
            stepSize: 2,
            enabled: true);

        Assert.False(idle.Moved);
        Assert.True(active.Moved);
    }

    [Fact]
    public void GetEffectiveRangeExpandsForExtendedPatrol()
    {
        var normal = PetRoamController.GetEffectiveRange(
            PetBehaviorMode.Patrol,
            baseRange: 180,
            availableWidth: 1000,
            extendedPatrolActive: false);
        var extended = PetRoamController.GetEffectiveRange(
            PetBehaviorMode.Patrol,
            baseRange: 180,
            availableWidth: 1000,
            extendedPatrolActive: true);

        Assert.Equal(180, normal);
        Assert.True(extended > normal);
        Assert.True(extended <= 560);
    }
}
