using System.Windows;
using System.Windows.Input;
using DesktopPets.App.Models;
using DesktopPets.App.Services;
using DesktopPets.App.Views;
using DesktopPets.App.ViewModels;

namespace DesktopPets.App;

public partial class MainWindow : Window
{
    private const double DragStartThreshold = 10.0;

    private readonly PetAnimationController _animations;
    private readonly AppPaths _paths;
    private readonly DragDirectionTracker _dragDirectionTracker = new();
    private readonly PetRoamController _roamController;
    private readonly MainWindowViewModel _viewModel;
    private PetManagerWindow? _managerWindow;
    private Point _dragStartScreenPoint;
    private Point _mouseDownPoint;
    private double _windowStartLeft;
    private double _windowStartTop;
    private bool _hasPendingClick;
    private bool _isDragging;

    public MainWindow()
    {
        InitializeComponent();

        _paths = new AppPaths();
        var configurationService = new ConfigurationService(_paths);
        var petAssetService = new PetAssetService(_paths);

        _viewModel = new MainWindowViewModel(configurationService, petAssetService);
        _animations = new PetAnimationController(PetTranslate, PetScale, PetRotate);
        _roamController = new PetRoamController(this, _viewModel);
        DataContext = _viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Load();

        if (_viewModel.WindowLeft is { } left)
        {
            Left = left;
        }

        if (_viewModel.WindowTop is { } top)
        {
            Top = top;
        }

        _animations.StartIdle();
        _roamController.ResetCenter(Left);
        _roamController.Start();
    }

    private void RootSurface_MouseEnter(object sender, MouseEventArgs e)
    {
        if (_isDragging || _viewModel.IsManualAnimationPreviewActive)
        {
            return;
        }

        _roamController.Suspend();
        _viewModel.PlayHoverAnimation();
    }

    private void RootSurface_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_isDragging || _hasPendingClick || _viewModel.IsManualAnimationPreviewActive)
        {
            return;
        }

        _viewModel.PlayIdle();
        _roamController.Resume();
    }

    private void RootSurface_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        _mouseDownPoint = e.GetPosition(this);
        _dragStartScreenPoint = PointToScreen(_mouseDownPoint);
        _dragDirectionTracker.Reset(_dragStartScreenPoint.X);
        _windowStartLeft = Left;
        _windowStartTop = Top;
        _hasPendingClick = true;
        _isDragging = false;
        _roamController.Suspend();
        RootSurface.CaptureMouse();
        e.Handled = true;
    }

    private void RootSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if ((!_hasPendingClick && !_isDragging) || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentScreenPoint = PointToScreen(e.GetPosition(this));
        var deltaX = currentScreenPoint.X - _dragStartScreenPoint.X;
        var deltaY = currentScreenPoint.Y - _dragStartScreenPoint.Y;

        if (!_isDragging && !HasStartedDrag(deltaX, deltaY))
        {
            return;
        }

        if (!_isDragging)
        {
            _isDragging = true;
            _hasPendingClick = false;
            _animations.SetDragging(true);
        }

        if (_dragDirectionTracker.Update(currentScreenPoint.X, deltaX) is { } movingRight)
        {
            _viewModel.PlayDragging(movingRight);
        }

        MoveWindowWithinVirtualScreen(_windowStartLeft + deltaX, _windowStartTop + deltaY);
        e.Handled = true;
    }

    private void RootSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        RootSurface.ReleaseMouseCapture();

        if (_isDragging)
        {
            _isDragging = false;
            _hasPendingClick = false;
            _animations.SetDragging(false);
            _viewModel.PlayIdle();
            _roamController.ResetCenter(Left);
            _roamController.Resume();
            SaveWindowState();
            e.Handled = true;
            return;
        }

        if (_hasPendingClick)
        {
            _hasPendingClick = false;
            _viewModel.PlayNextClickAnimation();
            _animations.PlayClick();
            _roamController.SuspendFor(TimeSpan.FromSeconds(3.5));
            e.Handled = true;
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = TryGetDroppedFiles(e, out var filePaths) &&
                    PetAssetService.IsSupportedDropPaths(filePaths)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (TryGetDroppedFiles(e, out var filePaths) && _viewModel.ImportPet(filePaths))
        {
            _roamController.ResetCenter(Left);
            _animations.PlayClick();
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _roamController.Stop();
        SaveWindowState();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ManageMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ShowManagerWindow();
    }

    private void ImportMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ShowManagerWindow();
        _managerWindow?.FocusManifestPicker();
    }

    private void OpenDataFolderMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _paths.EnsureBaseDirectories();
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _paths.RootDirectory,
            UseShellExecute = true
        });
    }

    private void AnimationMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string stateName })
        {
            _roamController.SuspendFor(TimeSpan.FromSeconds(4));
            _viewModel.PlayAnimationState(stateName);
        }
    }

    private void BehaviorModeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string behaviorMode })
        {
            _viewModel.BehaviorMode = behaviorMode;
            _roamController.ResetCenter(Left);
            _roamController.ResetBehaviorSchedule();
            if (PetBehaviorMode.Normalize(behaviorMode) == PetBehaviorMode.Still)
            {
                _viewModel.PlayIdle();
            }
        }
    }

    private void IncreaseScaleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IncreaseScale();
    }

    private void DecreaseScaleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DecreaseScale();
    }

    private void ResetScaleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetScale();
    }

    private void SaveWindowState()
    {
        _viewModel.SaveWindowState(Left, Top);
    }

    private void ShowManagerWindow()
    {
        if (_managerWindow is { IsVisible: true })
        {
            _managerWindow.Activate();
            return;
        }

        _managerWindow = new PetManagerWindow(_viewModel, _paths)
        {
            Owner = this
        };
        _managerWindow.Closed += (_, _) => _managerWindow = null;
        _managerWindow.Show();
    }

    private void MoveWindowWithinVirtualScreen(double left, double top)
    {
        var width = ActualWidth > 0 ? ActualWidth : 320;
        var height = ActualHeight > 0 ? ActualHeight : 320;
        var minLeft = SystemParameters.VirtualScreenLeft;
        var minTop = SystemParameters.VirtualScreenTop;
        var maxLeft = minLeft + SystemParameters.VirtualScreenWidth - width;
        var maxTop = minTop + SystemParameters.VirtualScreenHeight - height;

        Left = Math.Clamp(left, minLeft, Math.Max(minLeft, maxLeft));
        Top = Math.Clamp(top, minTop, Math.Max(minTop, maxTop));
    }

    private static bool HasStartedDrag(double deltaX, double deltaY)
    {
        return (deltaX * deltaX) + (deltaY * deltaY) >= DragStartThreshold * DragStartThreshold;
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
