namespace DesktopPets.App.Models;

public static class PetScaleSettings
{
    public const double Default = 1.0;
    public const double Minimum = 0.5;
    public const double Maximum = 2.0;
    public const double Step = 0.1;

    public static double Normalize(double scale)
    {
        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale < Minimum || scale > Maximum)
        {
            return Default;
        }

        return Snap(scale);
    }

    public static double ClampAndSnap(double scale)
    {
        return Snap(Math.Clamp(scale, Minimum, Maximum));
    }

    public static double Increase(double scale)
    {
        return ClampAndSnap(scale + Step);
    }

    public static double Decrease(double scale)
    {
        return ClampAndSnap(scale - Step);
    }

    private static double Snap(double scale)
    {
        return Math.Round(scale / Step, MidpointRounding.AwayFromZero) * Step;
    }
}
