namespace GeoCore.Units.Conversions;

/// <summary>
/// Provides methods for converting and normalising angles.
/// </summary>
public static class AngleConverter
{
    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    public static double DegreesToRadians(double degrees) =>
        degrees * Math.PI / 180.0;

    /// <summary>
    /// Converts an angle from radians to degrees.
    /// </summary>
    /// <param name="radians">The angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    public static double RadiansToDegrees(double radians) =>
        radians * 180.0 / Math.PI;

    /// <summary>
    /// Normalises an angle in degrees into the range [0, 360).
    /// </summary>
    /// <param name="degrees">The angle in degrees. May be negative or exceed 360.</param>
    /// <returns>The equivalent angle in the range [0, 360).</returns>
    /// <remarks>
    /// Use this for compass bearings, where 0 and 360 describe the same direction.
    /// </remarks>
    public static double NormalizeDegrees(double degrees)
    {
        if (double.IsNaN(degrees) || double.IsInfinity(degrees))
            return degrees;

        // Already in range: return it untouched rather than round-tripping it through
        // arithmetic that would perturb the last bit or two.
        if (degrees >= 0.0 && degrees < 360.0)
            return degrees;

        var result = degrees % 360.0;
        return result < 0 ? result + 360.0 : result;
    }

    /// <summary>
    /// Normalises a longitude in degrees into the range [-180, 180).
    /// </summary>
    /// <param name="degrees">The longitude in degrees. May be outside the valid range.</param>
    /// <returns>The equivalent longitude in the range [-180, 180).</returns>
    /// <remarks>
    /// A longitude of exactly 180 is mapped to -180, since both name the same meridian.
    /// </remarks>
    public static double NormalizeLongitude(double degrees)
    {
        if (double.IsNaN(degrees) || double.IsInfinity(degrees))
            return degrees;

        // Already in range: return it untouched. Adding and subtracting 180 would shift a
        // value like -0.1278 by an ulp, which is enough to break an exact bounds comparison.
        if (degrees >= -180.0 && degrees < 180.0)
            return degrees;

        var result = (degrees + 180.0) % 360.0;
        if (result < 0)
            result += 360.0;

        return result - 180.0;
    }

    /// <summary>
    /// Returns the smallest signed difference between two bearings, in degrees.
    /// </summary>
    /// <param name="fromDegrees">The starting bearing, in degrees.</param>
    /// <param name="toDegrees">The target bearing, in degrees.</param>
    /// <returns>
    /// The turn required to get from <paramref name="fromDegrees"/> to
    /// <paramref name="toDegrees"/>, in the range (-180, 180]. Positive values turn clockwise.
    /// </returns>
    public static double DifferenceBetweenBearings(double fromDegrees, double toDegrees)
    {
        var difference = NormalizeDegrees(toDegrees - fromDegrees);
        return difference > 180.0 ? difference - 360.0 : difference;
    }
}
