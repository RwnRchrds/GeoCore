namespace GeoCore.Units.Conversions;

/// <summary>
/// Provides methods for converting angles between degrees and radians.
/// </summary>
public static class AngleConverter
{
    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    public static double DegreesToRadians(double degrees) =>
        degrees * Math.PI / 180;

    /// <summary>
    /// Converts an angle from radians to degrees.
    /// </summary>
    /// <param name="radians">The angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    public static double RadiansToDegrees(double radians) =>
        radians * 180 / Math.PI;
}