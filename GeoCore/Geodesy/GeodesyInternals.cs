using GeoCore.Compatibility;
using GeoCore.Core;
using GeoCore.Units.Conversions;

namespace GeoCore.Geodesy
{
    /// <summary>
    /// Shared helpers for turning computed radian values back into valid points.
    /// </summary>
    internal static class GeodesyInternals
    {
        /// <summary>
        /// Builds a <see cref="GeoPoint"/> from radians, clamping and wrapping so that
        /// floating-point drift just past a pole or the antimeridian cannot throw.
        /// </summary>
        internal static GeoPoint PointFromRadians(double latitudeRadians, double longitudeRadians) =>
            new(
                MathCompat.Clamp(
                    AngleConverter.RadiansToDegrees(latitudeRadians),
                    GeoConstants.MinLatitude,
                    GeoConstants.MaxLatitude),
                AngleConverter.NormalizeLongitude(AngleConverter.RadiansToDegrees(longitudeRadians)));

        /// <summary>
        /// Shifts a longitude difference in radians into (-π, π].
        /// </summary>
        internal static double WrapLongitudeDelta(double deltaRadians)
        {
            if (deltaRadians > Math.PI)
                return deltaRadians - (2 * Math.PI);

            if (deltaRadians < -Math.PI)
                return deltaRadians + (2 * Math.PI);

            return deltaRadians;
        }
    }
}
