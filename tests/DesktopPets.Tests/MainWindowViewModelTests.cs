using DesktopPets.App.Models;
using DesktopPets.App.Services;
using DesktopPets.App.ViewModels;

namespace DesktopPets.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void SetScaleClampsAndSavesConfiguration()
    {
        using var workspace = new TempWorkspace();
        var paths = new AppPaths(workspace.Path);
        var viewModel = CreateViewModel(paths);

        viewModel.SetScale(3);

        Assert.Equal(PetScaleSettings.Maximum, viewModel.PetScale);
        Assert.Equal("200%", viewModel.ScalePercent);
        var saved = new ConfigurationService(paths).LoadOrCreate();
        Assert.Equal(PetScaleSettings.Maximum, saved.Scale);
    }

    [Fact]
    public void IncreaseAndDecreaseScaleUseConfiguredStep()
    {
        using var workspace = new TempWorkspace();
        var viewModel = CreateViewModel(new AppPaths(workspace.Path));

        viewModel.IncreaseScale();
        Assert.Equal(1.1, viewModel.PetScale);

        viewModel.DecreaseScale();
        Assert.Equal(1.0, viewModel.PetScale);
    }

    [Fact]
    public void ShowBubbleSetsVisibleMessage()
    {
        using var workspace = new TempWorkspace();
        var viewModel = CreateViewModel(new AppPaths(workspace.Path));

        viewModel.ShowBubble("今天也要加油。");

        Assert.True(viewModel.HasStatusMessage);
        Assert.Equal("今天也要加油。", viewModel.StatusMessage);
    }

    private static MainWindowViewModel CreateViewModel(AppPaths paths)
    {
        return new MainWindowViewModel(
            new ConfigurationService(paths),
            new PetAssetService(paths));
    }
}
