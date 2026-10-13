using DesktopPets.App.Services;

namespace DesktopPets.Tests;

public sealed class DragDirectionTrackerTests
{
    [Fact]
    public void UpdateKeepsLeftDirectionWhenScreenXDoesNotChange()
    {
        var tracker = new DragDirectionTracker();
        tracker.Reset(100);

        var first = tracker.Update(96, -4);
        var second = tracker.Update(96, -4);

        Assert.False(first);
        Assert.False(second);
    }

    [Fact]
    public void UpdateKeepsRightDirectionWhenScreenXDoesNotChange()
    {
        var tracker = new DragDirectionTracker();
        tracker.Reset(100);

        var first = tracker.Update(104, 4);
        var second = tracker.Update(104, 4);

        Assert.True(first);
        Assert.True(second);
    }

    [Fact]
    public void UpdateChangesDirectionAfterMeaningfulOppositeMovement()
    {
        var tracker = new DragDirectionTracker();
        tracker.Reset(100);

        var first = tracker.Update(106, 6);
        var stillRight = tracker.Update(105.5, 5.5);
        var changedLeft = tracker.Update(104.5, 4.5);

        Assert.True(first);
        Assert.True(stillRight);
        Assert.False(changedLeft);
    }
}
