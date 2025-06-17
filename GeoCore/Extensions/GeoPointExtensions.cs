using GeoCore.Core;
using GeoCore.Units;
using GeoCore.Units.Conversions;

namespace GeoCore.Extensions
{
    /// <summary>
    /// Provides geospatial calculations for GeoPoint.
    /// </summary>
    public static class GeoPointExtensions
    {
        /// <summary>
        /// Calculates the great-circle distance between two geographic points.
        /// </summary>
        /// <param name="from">The starting GeoPoint.</param>
        /// <param name="to">The destination GeoPoint.</param>
        /// <param name="unit">The unit of distance (default is kilometers).</param>
        /// <returns>The distance between the two points in the specified unit.</returns>
        public static double DistanceTo(this GeoPoint from, GeoPoint to, DistanceUnit unit = DistanceUnit.Kilometers)
        {
            var fromLat = AngleConverter.DegreesToRadians(from.Latitude);
            var fromLon = AngleConverter.DegreesToRadians(from.Longitude);
            var toLat = AngleConverter.DegreesToRadians(to.Latitude);
            var toLon = AngleConverter.DegreesToRadians(to.Longitude);

            var deltaLat = toLat - fromLat;
            var deltaLon = toLon - fromLon;

            var a = Math.Pow(Math.Sin(deltaLat / 2), 2) +
                    Math.Cos(fromLat) * Math.Cos(toLat) * Math.Pow(Math.Sin(deltaLon / 2), 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distanceKm = GeoConstants.EarthRadiusKm * c;

            return DistanceConverter.FromKilometers(distanceKm, unit);
        }

        /// <summary>
        /// Calculates the initial bearing (forward azimuth) from one GeoPoint to another.
        /// </summary>
        /// <param name="from">The starting GeoPoint.</param>
        /// <param name="to">The destination GeoPoint.</param>
        /// <returns>The bearing in degrees from north (0°–360°).</returns>
        public static double BearingTo(this GeoPoint from, GeoPoint to)
        {
            var fromLat = AngleConverter.DegreesToRadians(from.Latitude);
            var fromLon = AngleConverter.DegreesToRadians(from.Longitude);
            var toLat = AngleConverter.DegreesToRadians(to.Latitude);
            var toLon = AngleConverter.DegreesToRadians(to.Longitude);

            var deltaLon = toLon - fromLon;

            var y = Math.Sin(deltaLon) * Math.Cos(toLat);
            var x = Math.Cos(fromLat) * Math.Sin(toLat) -
                    Math.Sin(fromLat) * Math.Cos(toLat) * Math.Cos(deltaLon);

            var bearingRad = Math.Atan2(y, x);
            var bearingDeg = AngleConverter.RadiansToDegrees(bearingRad);

            // Normalize to 0°–360°
            return (bearingDeg + 360) % 360;
        }

        /// <summary>
        /// Moves from a starting GeoPoint in the specified direction and distance.
        /// </summary>
        /// <param name="point">The starting point.</param>
        /// <param name="distance">The distance to move.</param>
        /// <param name="bearingDegrees">The initial bearing (in degrees).</param>
        /// <param name="unit">The distance unit (default: kilometers).</param>
        /// <returns>A new GeoPoint at the destination location.</returns>
        public static GeoPoint Move(this GeoPoint point, double distance, double bearingDegrees,
            DistanceUnit unit = DistanceUnit.Kilometers)
        {
            double angularDistance = DistanceConverter.ToKilometers(distance, unit) / GeoConstants.EarthRadiusKm;
            double bearingRad = AngleConverter.DegreesToRadians(bearingDegrees);

            double fromLat = AngleConverter.DegreesToRadians(point.Latitude);
            double fromLon = AngleConverter.DegreesToRadians(point.Longitude);

            double toLat = Math.Asin(
                Math.Sin(fromLat) * Math.Cos(angularDistance) +
                Math.Cos(fromLat) * Math.Sin(angularDistance) * Math.Cos(bearingRad)
            );

            double toLon = fromLon + Math.Atan2(
                Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(fromLat),
                Math.Cos(angularDistance) - Math.Sin(fromLat) * Math.Sin(toLat)
            );

            // Normalize longitude to -180..+180
            toLon = (toLon + 3 * Math.PI) % (2 * Math.PI) - Math.PI;

            return new GeoPoint(
                AngleConverter.RadiansToDegrees(toLat),
                AngleConverter.RadiansToDegrees(toLon)
            );
        }

        /// <summary>
        /// Returns a bounding box that fully contains a circle of the specified radius around the point.
        /// </summary>
        /// <param name="point">The center of the box.</param>
        /// <param name="radius">The radius of the circle to enclose.</param>
        /// <param name="unit">The unit of the radius.</param>
        /// <returns>A GeoBoundingBox surrounding the point within the radius.</returns>
        public static GeoBoundingBox GetBoundingBox(this GeoPoint point, double radius,
            DistanceUnit unit = DistanceUnit.Kilometers)
        {
            // Convert to kilometers
            var radiusKm = DistanceConverter.ToKilometers(radius, unit);

            // 1 degree latitude ≈ 111.32 km
            var deltaLat = radiusKm / 111.32;

            // 1 degree longitude ≈ 111.32 * cos(latitude) km
            var latRad = AngleConverter.DegreesToRadians(point.Latitude);
            var deltaLon = radiusKm / (111.32 * Math.Cos(latRad));

            return new GeoBoundingBox(
                MinLatitude: point.Latitude - deltaLat,
                MinLongitude: point.Longitude - deltaLon,
                MaxLatitude: point.Latitude + deltaLat,
                MaxLongitude: point.Longitude + deltaLon
            );
        }

        /// <summary>
        /// Converts the GeoPoint to a string in DMS (Degrees, Minutes, Seconds) format with N/S/E/W.
        /// </summary>
        /// <param name="point">The point to format.</param>
        /// <returns>A formatted string like "51°30'26.4"N, 0°7'39.1"W"</returns>
        public static string ToDmsString(this GeoPoint point)
        {
            static string FormatDms(double decimalDegrees, bool isLatitude)
            {
                var degrees = Math.Floor(Math.Abs(decimalDegrees));
                var minutesFull = (Math.Abs(decimalDegrees) - degrees) * 60;
                var minutes = Math.Floor(minutesFull);
                var seconds = (minutesFull - minutes) * 60;

                var hemisphere = isLatitude
                    ? (decimalDegrees >= 0 ? "N" : "S")
                    : (decimalDegrees >= 0 ? "E" : "W");

                return $"{degrees:0}°{minutes:00}'{seconds:00.00}\"{hemisphere}";
            }

            var latStr = FormatDms(point.Latitude, isLatitude: true);
            var lonStr = FormatDms(point.Longitude, isLatitude: false);

            return $"{latStr}, {lonStr}";
        }
    }
}