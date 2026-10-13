using System.Windows;
using System.Windows.Threading;
using DesktopPets.App.Models;
using DesktopPets.App.ViewModels;

namespace DesktopPets.App.Services;

public sealed record RoamStepResult(double NextLeft, int Direction, bool Moved, bool HitBoundary);

public sealed class PetRoamController
{
    private const double PatrolStepSize = 1.4;
    private const double LoafingStepSize = 1.05;
    private const double LoafingRange = 90;
    private const double ExtendedPatrolMaxRange = 560;

    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private readonly MainWindowViewModel _viewModel;
    private readonly Window _window;
    private double _centerLeft;
    private int _direction = 1;
    private bool _isSuspended;
    private DateTimeOffset? _suspendUntil;
    private DateTimeOffset _nextExtendedPatrolAt;
    private DateTimeOffset? _extendedPatrolUntil;
    private DateTimeOffset _nextLoafingActionAt;
    private DateTimeOffset? _loafingMoveUntil;
    private string _lastBehaviorMode = PetBehaviorMode.Loafing;

    public PetRoamController(Window window, MainWindowViewModel viewModel)
    {
        _window = window;
        _viewModel = viewModel;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(45)
        };
        _timer.Tick += (_, _) => MoveOneStep();
    }

    public bool IsInteractionSuspended =>
        _isSuspended ||
        (_suspendUntil is { } suspendUntil && suspendUntil > DateTimeOffset.Now);

    public void Start()
    {
        ResetCenter(_window.Left);
        ResetBehaviorSchedule();
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void ResetCenter(double centerLeft)
    {
        _centerLeft = centerLeft;
    }

    public void ResetBehaviorSchedule()
    {
        var now = DateTimeOffset.Now;
        _lastBehaviorMode = PetBehaviorMode.Normalize(_viewModel.BehaviorMode);
        _nextExtendedPatrolAt = now.AddSeconds(RandomSeconds(20, 40));
        _extendedPatrolUntil = null;
        _nextLoafingActionAt = now.AddSeconds(RandomSeconds(10, 25));
        _loafingMoveUntil = null;
    }

    public void Suspend()
    {
        _isSuspended = true;
    }

    public void Resume()
    {
        _isSuspended = false;
        _suspendUntil = null;
        ResetCenter(_window.Left);
    }

    public void SuspendFor(TimeSpan duration)
    {
        _isSuspended = false;
        _suspendUntil = DateTimeOffset.Now.Add(duration);
    }

    public static RoamStepResult CalculateNextStep(
        double currentLeft,
        double windowWidth,
        double centerLeft,
        double range,
        double minLeft,
        double maxRight,
        int direction,
        double stepSize,
        bool enabled)
    {
        if (!enabled)
        {
            return new RoamStepResult(currentLeft, direction, Moved: false, HitBoundary: false);
        }

        var safeDirection = direction >= 0 ? 1 : -1;
        var leftLimit = Math.Max(minLeft, centerLeft - range);
        var rightLimit = Math.Min(maxRight - windowWidth, centerLeft + range);
        if (rightLimit < leftLimit)
        {
            var clamped = Math.Clamp(currentLeft, minLeft, Math.Max(minLeft, maxRight - windowWidth));
            return new RoamStepResult(clamped, safeDirection, Math.Abs(clamped - currentLeft) > 0.01, HitBoundary: true);
        }

        var nextLeft = currentLeft + (safeDirection * stepSize);
        var hitBoundary = false;
        if (nextLeft > rightLimit)
        {
            nextLeft = rightLimit;
            safeDirection = -1;
            hitBoundary = true;
        }
        else if (nextLeft < leftLimit)
        {
            nextLeft = leftLimit;
            safeDirection = 1;
            hitBoundary = true;
        }

        return new RoamStepResult(nextLeft, safeDirection, Math.Abs(nextLeft - currentLeft) > 0.01, hitBoundary);
    }

    public static RoamStepResult CalculateNextStepForBehavior(
        string behaviorMode,
        bool loafingMoveActive,
        bool extendedPatrolActive,
        double currentLeft,
        double windowWidth,
        double centerLeft,
        double baseRange,
        double minLeft,
        double maxRight,
        int direction,
        double stepSize,
        bool enabled)
    {
        if (!ShouldMoveForBehaviorMode(behaviorMode, loafingMoveActive))
        {
            return new RoamStepResult(currentLeft, direction, Moved: false, HitBoundary: false);
        }

        var range = GetEffectiveRange(
            behaviorMode,
            baseRange,
            Math.Max(0, maxRight - minLeft - windowWidth),
            extendedPatrolActive);

        return CalculateNextStep(
            currentLeft,
            windowWidth,
            centerLeft,
            range,
            minLeft,
            maxRight,
            direction,
            stepSize,
            enabled);
    }

    public static bool ShouldMoveForBehaviorMode(string behaviorMode, bool loafingMoveActive)
    {
        return PetBehaviorMode.Normalize(behaviorMode) switch
        {
            PetBehaviorMode.Still => false,
            PetBehaviorMode.Patrol => true,
            PetBehaviorMode.Loafing => loafingMoveActive,
            _ => false
        };
    }

    public static double GetEffectiveRange(
        string behaviorMode,
        double baseRange,
        double availableWidth,
        bool extendedPatrolActive)
    {
        var safeBaseRange = baseRange > 0 ? baseRange : 180;
        return PetBehaviorMode.Normalize(behaviorMode) switch
        {
            PetBehaviorMode.Patrol when extendedPatrolActive =>
                Math.Max(safeBaseRange, Math.Min(ExtendedPatrolMaxRange, availableWidth * 0.38)),
            PetBehaviorMode.Loafing => Math.Min(safeBaseRange, LoafingRange),
            _ => safeBaseRange
        };
    }

    private void MoveOneStep()
    {
        if (!_viewModel.IsAnimatedPetLoaded ||
            _viewModel.IsManualAnimationPreviewActive)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        var behaviorMode = PetBehaviorMode.Normalize(_viewModel.BehaviorMode);
        if (!string.Equals(_lastBehaviorMode, behaviorMode, StringComparison.OrdinalIgnoreCase))
        {
            ResetBehaviorSchedule();
            _lastBehaviorMode = behaviorMode;
            if (behaviorMode == PetBehaviorMode.Still)
            {
                _viewModel.PlayIdle();
            }
        }

        if (_isSuspended)
        {
            return;
        }

        if (_suspendUntil is { } suspendUntil)
        {
            if (suspendUntil > DateTimeOffset.Now)
            {
                return;
            }

            _suspendUntil = null;
        }

        var loafingMoveActive = false;
        var extendedPatrolActive = false;
        if (behaviorMode == PetBehaviorMode.Patrol)
        {
            extendedPatrolActive = UpdateExtendedPatrol(now);
        }
        else if (behaviorMode == PetBehaviorMode.Loafing)
        {
            loafingMoveActive = UpdateLoafingActivity(now);
        }

        var width = _window.ActualWidth > 0 ? _window.ActualWidth : 320;
        var stepSize = behaviorMode == PetBehaviorMode.Loafing ? LoafingStepSize : PatrolStepSize;
        var result = CalculateNextStepForBehavior(
            behaviorMode,
            loafingMoveActive,
            extendedPatrolActive,
            _window.Left,
            width,
            _centerLeft,
            _viewModel.AutoWalkRange,
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth,
            _direction,
            stepSize,
            enabled: true);

        _direction = result.Direction;
        if (!result.Moved)
        {
            return;
        }

        _window.Left = result.NextLeft;
        _viewModel.PlayRoamAnimation(_direction > 0);
    }

    private bool UpdateExtendedPatrol(DateTimeOffset now)
    {
        if (_extendedPatrolUntil is { } activeUntil)
        {
            if (activeUntil > now)
            {
                return true;
            }

            _extendedPatrolUntil = null;
            _nextExtendedPatrolAt = now.AddSeconds(RandomSeconds(20, 40));
            return false;
        }

        if (now < _nextExtendedPatrolAt)
        {
            return false;
        }

        _extendedPatrolUntil = now.AddSeconds(RandomSeconds(7, 12));
        _viewModel.ShowBubble("我去远一点巡逻啦。");
        return true;
    }

    private bool UpdateLoafingActivity(DateTimeOffset now)
    {
        if (_loafingMoveUntil is { } moveUntil)
        {
            if (moveUntil > now)
            {
                return true;
            }

            _loafingMoveUntil = null;
            _viewModel.PlayIdle();
            _nextLoafingActionAt = now.AddSeconds(RandomSeconds(10, 25));
            return false;
        }

        if (now < _nextLoafingActionAt)
        {
            return false;
        }

        if (_random.NextDouble() < 0.7)
        {
            _direction = _random.Next(2) == 0 ? -1 : 1;
            _loafingMoveUntil = now.AddSeconds(RandomSeconds(1.6, 3.4));
            _viewModel.ShowBubble("摸鱼时间，散步一下。");
            return true;
        }

        _viewModel.PlayAmbientAnimation(_random.Next(2) == 0 ? "waiting" : "review");
        _nextLoafingActionAt = now.AddSeconds(RandomSeconds(10, 25));
        return false;
    }

    private double RandomSeconds(double minimum, double maximum)
    {
        return minimum + (_random.NextDouble() * (maximum - minimum));
    }
}
