using DesktopPets.App.Models;

namespace DesktopPets.Tests;

public sealed class PetScaleSettingsTests
{
    [Fact]
    public void ClampAndSnapClampsSmallValuesToMinimum()
    {
        Assert.Equal(0.5, PetScaleSettings.ClampAndSnap(0.1));
    }

    [Fact]
    public void ClampAndSnapClampsLargeValuesToMaximum()
    {
        Assert.Equal(2.0, PetScaleSettings.ClampAndSnap(8));
    }

    [Fact]
    public void IncreaseAndDecreaseUseOneTenthSteps()
    {
        Assert.Equal(1.1, PetScaleSettings.Increase(1.0));
        Assert.Equal(0.9, PetScaleSettings.Decrease(1.0));
    }
}
