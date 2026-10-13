using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using DesktopPets.App.Services;
using DesktopPets.App.ViewModels;

namespace DesktopPets.App.Views;

public partial class PetManagerWindow : Window
{
    private readonly AppPaths _paths;
    private readonly MainWindowViewModel _viewModel;

    public PetManagerWindow(MainWindowViewModel viewModel, AppPaths paths)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _paths = paths;
        DataContext = _viewModel;
    }

    public void FocusManifestPicker()
    {
        Activate();
        ChooseManifest();
    }

    private void ChooseManifestButton_Click(object sender, RoutedEventArgs e)
    {
        ChooseManifest();
    }

    private void ChooseSpritesheetButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 spritesheet 文件",
            Filter = "Spritesheet 文件 (*.webp;*.png)|*.webp;*.png|所有文件 (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            SpritesheetPathBox.Text = dialog.FileName;
            StatusText.Text = "已选择 spritesheet 文件。";
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var files = BuildSelectedFiles();
        if (files.Count == 0)
        {
            StatusText.Text = "请选择 pet.json，或把宠物包拖入这个窗口。";
            return;
        }

        ImportFiles(files);
    }

    private void AnimationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string stateName })
        {
            return;
        }

        StatusText.Text = _viewModel.PlayAnimationState(stateName)
            ? $"正在播放动作：{GetAnimationDisplayName(stateName)}"
            : "请先加载 Codex 动画宠物。";
    }

    private void BackToIdleButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.PlayIdle();
        StatusText.Text = "已回到待机动作。";
    }

    private void OpenDataFolderButton_Click(object sender, RoutedEventArgs e)
    {
        _paths.EnsureBaseDirectories();
        Process.Start(new ProcessStartInfo
        {
            FileName = _paths.RootDirectory,
            UseShellExecute = true
        });
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = TryGetDroppedFiles(e, out var files) &&
                    PetAssetService.IsSupportedDropPaths(files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!TryGetDroppedFiles(e, out var files))
        {
            return;
        }

        ImportFiles(files);
    }

    private void ChooseManifest()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 pet.json",
            Filter = "宠物配置 (pet.json)|pet.json|JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            ManifestPathBox.Text = dialog.FileName;
            StatusText.Text = "已选择 pet.json。";
        }
    }

    private List<string> BuildSelectedFiles()
    {
        var files = new List<string>();

        if (!string.IsNullOrWhiteSpace(ManifestPathBox.Text))
        {
            files.Add(ManifestPathBox.Text);
        }

        if (!string.IsNullOrWhiteSpace(SpritesheetPathBox.Text))
        {
            files.Add(SpritesheetPathBox.Text);
        }

        return files;
    }

    private void ImportFiles(IReadOnlyList<string> files)
    {
        if (_viewModel.ImportPet(files))
        {
            StatusText.Text = "导入成功，宠物正在播放。";
            ManifestPathBox.Text = string.Empty;
            SpritesheetPathBox.Text = string.Empty;
            return;
        }

        StatusText.Text = "导入失败，请确认 pet.json 与 spritesheet 是否匹配。";
    }

    private static string GetAnimationDisplayName(string stateName)
    {
        return DesktopPets.App.Models.CodexSpriteSheetState.Options
            .FirstOrDefault(option => string.Equals(option.StateName, stateName, StringComparison.OrdinalIgnoreCase))
            ?.DisplayName ?? stateName;
    }

    private static bool TryGetDroppedFiles(DragEventArgs e, out IReadOnlyList<string> filePaths)
    {
        filePaths = [];

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
        {
            return false;
        }

        filePaths = files;
        return true;
    }
}
