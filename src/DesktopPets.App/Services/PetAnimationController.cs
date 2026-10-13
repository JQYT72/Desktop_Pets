using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DesktopPets.App.Services;

public sealed class PetAnimationController
{
    private readonly RotateTransform _rotate;
    private readonly ScaleTransform _scale;
    private readonly TranslateTransform _translate;
    private bool _isDragging;

    public PetAnimationController(TranslateTransform translate, ScaleTransform scale, RotateTransform rotate)
    {
        _translate = translate;
        _scale = scale;
        _rotate = rotate;
    }

    public void StartIdle()
    {
        if (_isDragging)
        {
            return;
        }

        ResetToRestPose();
    }

    public void SetDragging(bool isDragging)
    {
        _isDragging = isDragging;

        if (isDragging)
        {
            StopAnimations();
            _rotate.Angle = -5;
            _translate.Y = 4;
            return;
        }

        StopAnimations();
        _rotate.Angle = 0;
        _translate.Y = 0;
        StartIdle();
    }

    public void PlayClick()
    {
        if (_isDragging)
        {
            return;
        }

        StopAnimations();

        var bounce = new DoubleAnimation
        {
            From = 0,
            To = -42,
            Duration = TimeSpan.FromMilliseconds(160),
            AutoReverse = true,
            EasingFunction = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut }
        };

        var scaleUp = new DoubleAnimation
        {
            From = _scale.ScaleX,
            To = _scale.ScaleX * 1.22,
            Duration = TimeSpan.FromMilliseconds(160),
            AutoReverse = true,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        bounce.Completed += (_, _) =>
        {
            StartIdle();
        };

        _translate.BeginAnimation(TranslateTransform.YProperty, bounce);
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);
    }

    private void StopAnimations()
    {
        _translate.BeginAnimation(TranslateTransform.YProperty, null);
        _rotate.BeginAnimation(RotateTransform.AngleProperty, null);
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
    }

    private void ResetToRestPose()
    {
        StopAnimations();
        _translate.Y = 0;
        _rotate.Angle = 0;
        _scale.ScaleX = 1;
        _scale.ScaleY = 1;
    }
}
