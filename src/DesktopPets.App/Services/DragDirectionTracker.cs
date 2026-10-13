namespace DesktopPets.App.Services;

public sealed class DragDirectionTracker
{
    private const double DirectionChangeThreshold = 1.0;

    private double _lastTrackedScreenX;
    private bool? _movingRight;

    public void Reset(double screenX)
    {
        _lastTrackedScreenX = screenX;
        _movingRight = null;
    }

    public bool? Update(double screenX, double totalDeltaX)
    {
        var stepDeltaX = screenX - _lastTrackedScreenX;
        if (Math.Abs(stepDeltaX) >= DirectionChangeThreshold)
        {
            _movingRight = stepDeltaX > 0;
            _lastTrackedScreenX = screenX;
        }
        else if (_movingRight is null && Math.Abs(totalDeltaX) >= DirectionChangeThreshold)
        {
            _movingRight = totalDeltaX > 0;
        }

        return _movingRight;
    }
}
