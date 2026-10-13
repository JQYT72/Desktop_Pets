using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopPets.App.Models;
using DesktopPets.App.Services;

namespace DesktopPets.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly Uri SamplePetUri = new("pack://application:,,,/Assets/sample-pet.png");
    private static readonly string[] HoverBubbleMessages =
    [
        "来啦，今天也一起加油。",
        "摸摸头，精神恢复一点点。",
        "我在这里陪你。"
    ];

    private static readonly string[] ClickBubbleMessages =
    [
        "收到，换个动作给你看。",
        "嘿，我还醒着呢。",
        "今天也要开心一点。"
    ];

    private static readonly string[] LoafingBubbleMessages =
    [
        "我偷偷摸鱼走两步。",
        "休息一下也很重要。",
        "在桌面散散步。"
    ];

    private readonly ConfigurationService _configurationService;
    private readonly CodexSpriteSheetDecoder _spriteSheetDecoder = new();
    private readonly PetAnimationCycle _clickAnimationCycle = new();
    private readonly PetAssetService _petAssetService;
    private readonly DispatcherTimer _animationTimer;
    private readonly DispatcherTimer _statusTimer;
    private readonly Random _bubbleRandom = new();
    private readonly Dictionary<string, IReadOnlyList<ImageSource>> _spriteStateCache = new(StringComparer.OrdinalIgnoreCase);
    private AppConfiguration _configuration = AppConfiguration.CreateDefault();
    private PetAsset? _currentPetAsset;
    private string _currentPetName = "未加载宠物";
    private ImageSource? _petImageSource;
    private string? _statusMessage;
    private IReadOnlyList<ImageSource> _activeFrames = [];
    private int _activeFrameIndex;
    private bool _returnToIdleAfterState;
    private bool _loopActiveState;
    private bool _isAnimatedPetLoaded;
    private string _currentAnimationState = "idle";
    private DateTimeOffset _manualPreviewUntil;

    public MainWindowViewModel(ConfigurationService configurationService, PetAssetService petAssetService)
    {
        _configurationService = configurationService;
        _petAssetService = petAssetService;
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2.5)
        };
        _statusTimer.Tick += (_, _) =>
        {
            _statusTimer.Stop();
            StatusMessage = null;
        };

        _animationTimer = new DispatcherTimer();
        _animationTimer.Tick += (_, _) => AdvanceSpriteFrame();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<PetAnimationOption> AnimationOptions => CodexSpriteSheetState.Options;

    public IReadOnlyList<PetBehaviorModeOption> BehaviorModeOptions => PetBehaviorMode.Options;

    public ImageSource? PetImageSource
    {
        get => _petImageSource;
        private set => SetField(ref _petImageSource, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetField(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool IsAnimatedPetLoaded
    {
        get => _isAnimatedPetLoaded;
        private set => SetField(ref _isAnimatedPetLoaded, value);
    }

    public bool IsAutoWalkEnabled
    {
        get => PetBehaviorMode.AllowsAutomaticMovement(_configuration.BehaviorMode);
        set
        {
            BehaviorMode = value ? PetBehaviorMode.Loafing : PetBehaviorMode.Still;
        }
    }

    public double AutoWalkRange => _configuration.AutoWalkRange;

    public string BehaviorMode
    {
        get => PetBehaviorMode.Normalize(_configuration.BehaviorMode);
        set
        {
            var normalized = PetBehaviorMode.Normalize(value);
            if (string.Equals(BehaviorMode, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _configuration.BehaviorMode = normalized;
            _configuration.AutoWalkEnabled = PetBehaviorMode.AllowsAutomaticMovement(normalized);
            _configurationService.Save(_configuration);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAutoWalkEnabled));
            ShowBubble($"已切换到{PetBehaviorMode.GetDisplayName(normalized)}模式");
        }
    }

    public string CurrentAnimationState
    {
        get => _currentAnimationState;
        private set => SetField(ref _currentAnimationState, value);
    }

    public bool IsManualAnimationPreviewActive => _manualPreviewUntil > DateTimeOffset.Now;

    public string CurrentPetName
    {
        get => _currentPetName;
        private set => SetField(ref _currentPetName, value);
    }

    public double PetScale
    {
        get => _configuration.Scale;
        set => SetScale(value);
    }

    public string ScalePercent => $"{Math.Round(PetScale * 100):0}%";

    public double PetImageMaxSize => 280 * PetScale;

    public double PetSurfaceWidth => Math.Max(360, PetImageMaxSize + 120);

    public double PetSurfaceHeight => Math.Max(340, PetImageMaxSize + 90);

    public double? WindowLeft => _configuration.WindowLeft;

    public double? WindowTop => _configuration.WindowTop;

    public void Load()
    {
        _configuration = _configurationService.LoadOrCreate();
        OnScalePropertiesChanged();
        OnPropertyChanged(nameof(IsAutoWalkEnabled));
        OnPropertyChanged(nameof(BehaviorMode));

        if (!string.IsNullOrWhiteSpace(_configuration.CurrentPetManifest) &&
            _petAssetService.TryLoadPetAsset(_configuration.CurrentPetManifest, out var petAsset, out _))
        {
            LoadPetAsset(petAsset!);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_configuration.CurrentPetManifest))
        {
            ShowStatus("上次保存的宠物无法加载。", autoHide: false);
        }

        CurrentPetName = "示例宠物";
        IsAnimatedPetLoaded = false;
        StopSpriteAnimation();
        PetImageSource = CreateImage(SamplePetUri);
    }

    public bool ImportPet(string imagePath)
    {
        return ImportPet([imagePath]);
    }

    public bool ImportPet(IReadOnlyList<string> paths)
    {
        try
        {
            var petAsset = _petAssetService.ImportDroppedPaths(paths);
            if (!LoadPetAsset(petAsset))
            {
                ShowStatus("导入的宠物无法显示。", autoHide: false);
                return false;
            }

            _configuration.CurrentPetManifest = petAsset.ManifestPath;
            _configurationService.Save(_configuration);
            ShowBubble(petAsset.Kind == PetAssetKind.CodexSpriteSheet
                ? $"已加载动画宠物：{petAsset.DisplayName}"
                : "宠物已加载。");
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is FileNotFoundException ||
            exception is IOException ||
            exception is NotSupportedException ||
            exception is UnauthorizedAccessException)
        {
            ShowStatus(exception.Message, autoHide: false);
            return false;
        }
    }

    public void SaveWindowState(double left, double top)
    {
        _configuration.WindowLeft = left;
        _configuration.WindowTop = top;
        _configurationService.Save(_configuration);
    }

    public void IncreaseScale()
    {
        SetScale(PetScaleSettings.Increase(PetScale));
    }

    public void DecreaseScale()
    {
        SetScale(PetScaleSettings.Decrease(PetScale));
    }

    public void ResetScale()
    {
        SetScale(PetScaleSettings.Default);
    }

    public void SetScale(double scale)
    {
        var normalized = PetScaleSettings.ClampAndSnap(scale);
        if (Math.Abs(_configuration.Scale - normalized) < 0.001)
        {
            return;
        }

        _configuration.Scale = normalized;
        _configurationService.Save(_configuration);
        OnScalePropertiesChanged();
        ShowBubble($"大小已调整为 {ScalePercent}");
    }

    public void PlayIdle()
    {
        _manualPreviewUntil = default;
        PlaySpriteState("idle", loop: true, returnToIdle: false);
    }

    public void PlayClick()
    {
        PlaySpriteState("waving", loop: false, returnToIdle: true);
    }

    public string PlayNextClickAnimation()
    {
        var stateName = _clickAnimationCycle.NextDifferentFrom(CurrentAnimationState);
        _manualPreviewUntil = DateTimeOffset.Now.AddSeconds(3.5);
        PlaySpriteState(stateName, loop: true, returnToIdle: false);
        ShowBubble($"{Pick(ClickBubbleMessages)} 现在是{GetAnimationDisplayName(stateName)}。");
        return stateName;
    }

    public bool PlayHoverAnimation()
    {
        var played = PlaySpriteState("waving", loop: false, returnToIdle: true);
        ShowBubble(Pick(HoverBubbleMessages));
        return played;
    }

    public bool PlayAmbientAnimation(string stateName)
    {
        var played = PlaySpriteState(stateName, loop: false, returnToIdle: true);
        ShowBubble(Pick(LoafingBubbleMessages));
        return played;
    }

    public void PlayDragging(bool movingRight)
    {
        PlayLoopingStateIfChanged(movingRight ? "running-right" : "running-left");
    }

    public void PlayRoamAnimation(bool movingRight)
    {
        PlayLoopingStateIfChanged(movingRight ? "running-right" : "running-left");
    }

    public bool PlayAnimationState(string stateName)
    {
        return PlayAnimationState(stateName, loop: true);
    }

    public bool PlayAnimationState(string stateName, bool loop, bool showStatus = true)
    {
        var played = PlaySpriteState(stateName, loop: loop, returnToIdle: false);
        if (showStatus)
        {
            _manualPreviewUntil = DateTimeOffset.Now.AddSeconds(4);
            ShowBubble(played ? $"正在播放：{GetAnimationDisplayName(stateName)}" : "请先加载动画宠物。");
        }

        return played;
    }

    private bool LoadPetAsset(PetAsset petAsset)
    {
        _currentPetAsset = petAsset;
        CurrentPetName = petAsset.DisplayName;
        IsAnimatedPetLoaded = petAsset.Kind == PetAssetKind.CodexSpriteSheet;
        _spriteStateCache.Clear();
        _clickAnimationCycle.Reset();

        if (petAsset.Kind == PetAssetKind.CodexSpriteSheet)
        {
            return PlaySpriteState("idle", loop: true, returnToIdle: false);
        }

        StopSpriteAnimation();
        return TrySetPetImageFromFile(petAsset.ImagePath);
    }

    private void PlayLoopingStateIfChanged(string stateName)
    {
        if (IsLoopingStateActive(stateName))
        {
            return;
        }

        PlaySpriteState(stateName, loop: true, returnToIdle: false);
    }

    private bool IsLoopingStateActive(string stateName)
    {
        return string.Equals(CurrentAnimationState, stateName, StringComparison.OrdinalIgnoreCase) &&
               _loopActiveState &&
               _animationTimer.IsEnabled &&
               _activeFrames.Count > 0;
    }

    private bool PlaySpriteState(string stateName, bool loop, bool returnToIdle)
    {
        if (_currentPetAsset is not { Kind: PetAssetKind.CodexSpriteSheet })
        {
            return false;
        }

        try
        {
            if (!_spriteStateCache.TryGetValue(stateName, out var frames))
            {
                frames = _spriteSheetDecoder.DecodeState(_currentPetAsset.ImagePath, stateName).Cast<ImageSource>().ToList();
                _spriteStateCache[stateName] = frames;
            }

            if (frames.Count == 0)
            {
                return false;
            }

            var state = CodexSpriteSheetState.States.TryGetValue(stateName, out var found)
                ? found
                : CodexSpriteSheetState.States["idle"];

            _activeFrames = frames;
            _activeFrameIndex = 0;
            _returnToIdleAfterState = returnToIdle;
            _loopActiveState = loop;
            CurrentAnimationState = stateName;
            PetImageSource = _activeFrames[_activeFrameIndex];
            _animationTimer.Interval = TimeSpan.FromMilliseconds(state.FrameDurationMs);
            _animationTimer.Start();
            return true;
        }
        catch (Exception exception) when (
            exception is FileNotFoundException ||
            exception is IOException ||
            exception is NotSupportedException ||
            exception is InvalidOperationException)
        {
            StopSpriteAnimation();
            ShowStatus(exception.Message, autoHide: false);
            return false;
        }
    }

    private void AdvanceSpriteFrame()
    {
        if (_activeFrames.Count == 0)
        {
            StopSpriteAnimation();
            return;
        }

        _activeFrameIndex++;
        if (_activeFrameIndex >= _activeFrames.Count)
        {
            if (_returnToIdleAfterState)
            {
                PlaySpriteState("idle", loop: true, returnToIdle: false);
                return;
            }

            if (_loopActiveState)
            {
                _activeFrameIndex = 0;
            }
            else
            {
                StopSpriteAnimation();
                return;
            }
        }

        PetImageSource = _activeFrames[_activeFrameIndex];
    }

    private void StopSpriteAnimation()
    {
        _animationTimer.Stop();
        _activeFrames = [];
        _activeFrameIndex = 0;
        _returnToIdleAfterState = false;
        _loopActiveState = false;
    }

    private bool TrySetPetImageFromFile(string imagePath)
    {
        try
        {
            PetImageSource = CreateImage(new Uri(imagePath, UriKind.Absolute));
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public void ShowBubble(string message, TimeSpan? duration = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _statusTimer.Stop();
        _statusTimer.Interval = duration ?? TimeSpan.FromSeconds(2.5);
        StatusMessage = message;
        _statusTimer.Start();
    }

    private void ShowStatus(string message, bool autoHide = true)
    {
        _statusTimer.Stop();
        StatusMessage = message;

        if (autoHide)
        {
            _statusTimer.Start();
        }
    }

    private void OnScalePropertiesChanged()
    {
        OnPropertyChanged(nameof(PetScale));
        OnPropertyChanged(nameof(ScalePercent));
        OnPropertyChanged(nameof(PetImageMaxSize));
        OnPropertyChanged(nameof(PetSurfaceWidth));
        OnPropertyChanged(nameof(PetSurfaceHeight));
    }

    private string Pick(IReadOnlyList<string> messages)
    {
        return messages[_bubbleRandom.Next(messages.Count)];
    }

    private static string GetAnimationDisplayName(string stateName)
    {
        return CodexSpriteSheetState.Options
            .FirstOrDefault(option => string.Equals(option.StateName, stateName, StringComparison.OrdinalIgnoreCase))
            ?.DisplayName ?? stateName;
    }

    private static BitmapImage CreateImage(Uri uri)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        image.UriSource = uri;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
