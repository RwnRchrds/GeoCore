using GeoCore.Compatibility;
using GeoCore.Core;
using GeoCore.Units.Conversions;

namespace GeoCore.Geodesy
{
    /// <summary>
    /// Great-circle and rhumb-line calculations on a sphere of radius
    /// <see cref="GeoConstants.EarthRadiusKm"/>.
    /// </summary>
    /// <remarks>
    /// Spherical formulae are fast and simple, and are accurate to roughly 0.5% of the
    /// distance measured. Where that matters, use <see cref="Vincenty"/>, which models the
    /// WGS-84 ellipsoid and is accurate to well under a millimetre.
    /// </remarks>
    public static class Spherical
    {
        /// <summary>
        /// Returns the great-circle distance between two points, in metres, using the
        /// haversine formula.
        /// </summary>
        /// <param name="from">The first point.</param>
        /// <param name="to">The second point.</param>
        /// <returns>The distance in metres.</returns>
        public static double DistanceMeters(GeoPoint from, GeoPoint to) =>
            AngularDistanceRadians(from, to) * GeoConstants.EarthRadiusMeters;

        /// <summary>
        /// Returns the angular distance between two points, in radians.
        /// </summary>
        /// <param name="from">The first point.</param>
        /// <param name="to">The second point.</param>
        /// <returns>The angular separation in radians, between 0 and π.</returns>
        public static double AngularDistanceRadians(GeoPoint from, GeoPoint to)
        {
            var lat1 = AngleConverter.DegreesToRadians(from.Latitude);
            var lat2 = AngleConverter.DegreesToRadians(to.Latitude);
            var deltaLat = lat2 - lat1;
            var deltaLon = AngleConverter.DegreesToRadians(to.Longitude - from.Longitude);

            var sinHalfLat = Math.Sin(deltaLat / 2);
            var sinHalfLon = Math.Sin(deltaLon / 2);

            var a = (sinHalfLat * sinHalfLat) +
                    (Math.Cos(lat1) * Math.Cos(lat2) * sinHalfLon * sinHalfLon);

            // Clamp guards against a from rounding marginally above 1 for antipodal points.
            return 2 * Math.Asin(Math.Sqrt(MathCompat.Clamp(a, 0.0, 1.0)));
        }

        /// <summary>
        /// Returns the initial bearing (forward azimuth) along the great circle from one
        /// point to another.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <returns>The bearing in degrees clockwise from true north, in [0, 360).</returns>
        /// <remarks>
        /// Unlike a rhumb line, the bearing of a great circle changes as you travel it. Use
        /// <see cref="FinalBearingDegrees"/> for the bearing on arrival.
        /// </remarks>
        public static double InitialBearingDegrees(GeoPoint from, GeoPoint to)
        {
            var lat1 = AngleConverter.DegreesToRadians(from.Latitude);
            var lat2 = AngleConverter.DegreesToRadians(to.Latitude);
            var deltaLon = AngleConverter.DegreesToRadians(to.Longitude - from.Longitude);

            var y = Math.Sin(deltaLon) * Math.Cos(lat2);
            var x = (Math.Cos(lat1) * Math.Sin(lat2)) -
                    (Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon));

            return AngleConverter.NormalizeDegrees(
                AngleConverter.RadiansToDegrees(Math.Atan2(y, x)));
        }

        /// <summary>
        /// Returns the final bearing along the great circle arriving at <paramref name="to"/>.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <returns>The bearing in degrees clockwise from true north, in [0, 360).</returns>
        public static double FinalBearingDegrees(GeoPoint from, GeoPoint to) =>
            AngleConverter.NormalizeDegrees(InitialBearingDegrees(to, from) + 180.0);

        /// <summary>
        /// Returns the point at a given distance and bearing from a starting point, following
        /// a great circle.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="distanceMeters">The distance to travel, in metres. May be negative to travel backwards.</param>
        /// <param name="bearingDegrees">The initial bearing, in degrees clockwise from true north.</param>
        /// <returns>The destination point.</returns>
        public static GeoPoint Destination(GeoPoint from, double distanceMeters, double bearingDegrees)
        {
            var angularDistance = distanceMeters / GeoConstants.EarthRadiusMeters;
            var bearing = AngleConverter.DegreesToRadians(bearingDegrees);

            var lat1 = AngleConverter.DegreesToRadians(from.Latitude);
            var lon1 = AngleConverter.DegreesToRadians(from.Longitude);

            var sinLat1 = Math.Sin(lat1);
            var cosLat1 = Math.Cos(lat1);
            var sinDistance = Math.Sin(angularDistance);
            var cosDistance = Math.Cos(angularDistance);

            var sinLat2 = (sinLat1 * cosDistance) + (cosLat1 * sinDistance * Math.Cos(bearing));
            var lat2 = Math.Asin(MathCompat.Clamp(sinLat2, -1.0, 1.0));

            var lon2 = lon1 + Math.Atan2(
                Math.Sin(bearing) * sinDistance * cosLat1,
                cosDistance - (sinLat1 * Math.Sin(lat2)));

            return GeodesyInternals.PointFromRadians(lat2, lon2);
        }

        /// <summary>
        /// Returns the half-way point along the great circle between two points.
        /// </summary>
        /// <param name="from">The first point.</param>
        /// <param name="to">The second point.</param>
        /// <returns>The midpoint.</returns>
        public static GeoPoint Midpoint(GeoPoint from, GeoPoint to)
        {
            var lat1 = AngleConverter.DegreesToRadians(from.Latitude);
            var lon1 = AngleConverter.DegreesToRadians(from.Longitude);
            var lat2 = AngleConverter.DegreesToRadians(to.Latitude);
            var deltaLon = AngleConverter.DegreesToRadians(to.Longitude - from.Longitude);

            var cosLat2 = Math.Cos(lat2);
            var bx = cosLat2 * Math.Cos(deltaLon);
            var by = cosLat2 * Math.Sin(deltaLon);

            var cosLat1PlusBx = Math.Cos(lat1) + bx;

            var lat = Math.Atan2(
                Math.Sin(lat1) + Math.Sin(lat2),
                Math.Sqrt((cosLat1PlusBx * cosLat1PlusBx) + (by * by)));

            var lon = lon1 + Math.Atan2(by, cosLat1PlusBx);

            return GeodesyInternals.PointFromRadians(lat, lon);
        }

        /// <summary>
        /// Returns the point a given fraction of the way along the great circle between two
        /// points.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <param name="fraction">
        /// The fraction of the way along the arc. 0 returns <paramref name="from"/> and 1
        /// returns <paramref name="to"/>. Values outside [0, 1] extrapolate along the same
        /// great circle.
        /// </param>
        /// <returns>The interpolated point.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fraction"/> is NaN or infinite.</exception>
        /// <remarks>
        /// This is true spherical interpolation, not a linear blend of latitude and
        /// longitude, so the result lies on the shortest path between the two points.
        /// </remarks>
        public static GeoPoint IntermediatePoint(GeoPoint from, GeoPoint to, double fraction)
        {
            if (double.IsNaN(fraction) || double.IsInfinity(fraction))
                throw new ArgumentOutOfRangeException(nameof(fraction), fraction,
                    "Fraction must be a finite number.");

            var angularDistance = AngularDistanceRadians(from, to);

            // Coincident (or effectively coincident) points have no defined arc to walk.
            if (angularDistance < 1e-12)
                return from;

            var sinDistance = Math.Sin(angularDistance);
            var a = Math.Sin((1 - fraction) * angularDistance) / sinDistance;
            var b = Math.Sin(fraction * angularDistance) / sinDistance;

            var lat1 = AngleConverter.DegreesToRadians(from.Latitude);
            var lon1 = AngleConverter.DegreesToRadians(from.Longitude);
            var lat2 = AngleConverter.DegreesToRadians(to.Latitude);
            var lon2 = AngleConverter.DegreesToRadians(to.Longitude);

            var cosLat1 = Math.Cos(lat1);
            var cosLat2 = Math.Cos(lat2);

            var x = (a * cosLat1 * Math.Cos(lon1)) + (b * cosLat2 * Math.Cos(lon2));
            var y = (a * cosLat1 * Math.Sin(lon1)) + (b * cosLat2 * Math.Sin(lon2));
            var z = (a * Math.Sin(lat1)) + (b * Math.Sin(lat2));

            return GeodesyInternals.PointFromRadians(
                Math.Atan2(z, Math.Sqrt((x * x) + (y * y))),
                Math.Atan2(y, x));
        }

        /// <summary>
        /// Returns the signed distance of a point from the great circle passing through two
        /// other points.
        /// </summary>
        /// <param name="point">The point to measure.</param>
        /// <param name="pathStart">The start of the path.</param>
        /// <param name="pathEnd">The end of the path.</param>
        /// <returns>
        /// The perpendicular distance in metres. Positive values lie to the left of the path,
        /// negative values to the right.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="pathStart"/> and <paramref name="pathEnd"/> are the same point.</exception>
        /// <remarks>
        /// The path is treated as an infinite great circle, not a bounded segment. To measure
        /// against a segment, combine this with <see cref="AlongTrackDistanceMeters"/> to
        /// check whether the foot of the perpendicular falls inside the segment.
        /// </remarks>
        public static double CrossTrackDistanceMeters(GeoPoint point, GeoPoint pathStart, GeoPoint pathEnd)
        {
            if (pathStart == pathEnd)
                throw new ArgumentException(
                    "The path start and end must be different points to define a great circle.",
                    nameof(pathEnd));

            var angularDistance = AngularDistanceRadians(pathStart, point);
            var bearingToPoint = AngleConverter.DegreesToRadians(InitialBearingDegrees(pathStart, point));
            var bearingToEnd = AngleConverter.DegreesToRadians(InitialBearingDegrees(pathStart, pathEnd));

            var sinCrossTrack = Math.Sin(angularDistance) * Math.Sin(bearingToPoint - bearingToEnd);

            return Math.Asin(MathCompat.Clamp(sinCrossTrack, -1.0, 1.0)) * GeoConstants.EarthRadiusMeters;
        }

        /// <summary>
        /// Returns how far along a path the closest approach to a point lies.
        /// </summary>
        /// <param name="point">The point to measure.</param>
        /// <param name="pathStart">The start of the path.</param>
        /// <param name="pathEnd">The end of the path.</param>
        /// <returns>
        /// The distance in metres from <paramref name="pathStart"/> to the foot of the
        /// perpendicular from <paramref name="point"/>. Negative if that foot lies behind the
        /// start of the path.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="pathStart"/> and <paramref name="pathEnd"/> are the same point.</exception>
        public static double AlongTrackDistanceMeters(GeoPoint point, GeoPoint pathStart, GeoPoint pathEnd)
        {
            if (pathStart == pathEnd)
                throw new ArgumentException(
                    "The path start and end must be different points to define a great circle.",
                    nameof(pathEnd));

            var angularDistance = AngularDistanceRadians(pathStart, point);
            var bearingToPoint = AngleConverter.DegreesToRadians(InitialBearingDegrees(pathStart, point));
            var bearingToEnd = AngleConverter.DegreesToRadians(InitialBearingDegrees(pathStart, pathEnd));

            var crossTrackAngle = Math.Asin(MathCompat.Clamp(
                Math.Sin(angularDistance) * Math.Sin(bearingToPoint - bearingToEnd), -1.0, 1.0));

            var cosCrossTrack = Math.Cos(crossTrackAngle);
            if (Math.Abs(cosCrossTrack) < 1e-15)
                return 0.0;

            var alongTrackAngle = Math.Acos(
                MathCompat.Clamp(Math.Cos(angularDistance) / cosCrossTrack, -1.0, 1.0));

            // acos loses the sign, so recover it from which side of the start we are on.
            var sign = Math.Cos(bearingToEnd - bearingToPoint) < 0 ? -1.0 : 1.0;

            return sign * alongTrackAngle * GeoConstants.EarthRadiusMeters;
        }

        /// <summary>
        /// Returns the distance along a rhumb line (a path of constant bearing) between two
        /// points, in metres.
        /// </summary>
        /// <param name="from">The first point.</param>
        /// <param name="to">The second point.</param>
        /// <returns>The distance in metres.</returns>
        /// <remarks>
        /// A rhumb line is longer than the great circle between the same two points, but is
        /// easier to navigate because the bearing never changes.
        /// </remarks>
        public static double RhumbDistanceMeters(GeoPoint from, GeoPoint to)
        {
            var lat1 = AngleConverter.DegreesToRadians(from.Latitude);
            var lat2 = AngleConverter.DegreesToRadians(to.Latitude);
            var deltaLat = lat2 - lat1;

            var deltaLon = GeodesyInternals.WrapLongitudeDelta(
                AngleConverter.DegreesToRadians(to.Longitude - from.Longitude));

            var deltaPsi = MercatorStretch(lat2) - MercatorStretch(lat1);

            // q is the ratio of northing to latitude change; it degenerates on an
            // east-west course, where the stretch is zero.
            var q = Math.Abs(deltaPsi) > 1e-12 ? deltaLat / deltaPsi : Math.Cos(lat1);

            var qDeltaLon = q * deltaLon;

            return Math.Sqrt((deltaLat * deltaLat) + (qDeltaLon * qDeltaLon)) *
                   GeoConstants.EarthRadiusMeters;
        }

        /// <summary>
        /// Returns the constant bearing of the rhumb line between two points.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <returns>The bearing in degrees clockwise from true north, in [0, 360).</returns>
        public static double RhumbBearingDegrees(GeoPoint from, GeoPoint to)
        {
            var lat1 = AngleConverter.DegreesToRadians(from.Latitude);
            var lat2 = AngleConverter.DegreesToRadians(to.Latitude);

            var deltaLon = GeodesyInternals.WrapLongitudeDelta(
                AngleConverter.DegreesToRadians(to.Longitude - from.Longitude));

            var deltaPsi = MercatorStretch(lat2) - MercatorStretch(lat1);

            return AngleConverter.NormalizeDegrees(
                AngleConverter.RadiansToDegrees(Math.Atan2(deltaLon, deltaPsi)));
        }

        /// <summary>
        /// Returns the point reached by travelling a given distance along a constant bearing.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="distanceMeters">The distance to travel, in metres.</param>
        /// <param name="bearingDegrees">The constant bearing, in degrees clockwise from true north.</param>
        /// <returns>The destination point.</returns>
        public static GeoPoint RhumbDestination(GeoPoint from, double distanceMeters, double bearingDegrees)
        {
            var angularDistance = distanceMeters / GeoConstants.EarthRadiusMeters;
            var bearing = AngleConverter.DegreesToRadians(bearingDegrees);

            var lat1 = AngleConverter.DegreesToRadians(from.Latitude);
            var lon1 = AngleConverter.DegreesToRadians(from.Longitude);

            var deltaLat = angularDistance * Math.Cos(bearing);
            var lat2 = lat1 + deltaLat;

            // A rhumb line that runs into a pole reflects back down the far side.
            if (Math.Abs(lat2) > Math.PI / 2)
                lat2 = lat2 > 0 ? Math.PI - lat2 : -Math.PI - lat2;

            var deltaPsi = MercatorStretch(lat2) - MercatorStretch(lat1);
            var q = Math.Abs(deltaPsi) > 1e-12 ? deltaLat / deltaPsi : Math.Cos(lat1);

            var deltaLon = angularDistance * Math.Sin(bearing) / q;

            return GeodesyInternals.PointFromRadians(lat2, lon1 + deltaLon);
        }

        /// <summary>
        /// Returns the half-way point along the rhumb line between two points.
        /// </summary>
        /// <param name="from">The first point.</param>
        /// <param name="to">The second point.</param>
        /// <returns>The rhumb midpoint.</returns>
        public static GeoPoint RhumbMidpoint(GeoPoint from, GeoPoint to)
        {
            var lat1 = AngleConverter.DegreesToRadians(from.Latitude);
            var lat2 = AngleConverter.DegreesToRadians(to.Latitude);
            var lon1 = AngleConverter.DegreesToRadians(from.Longitude);
            var lon2 = AngleConverter.DegreesToRadians(to.Longitude);

            // Take the shorter way round when the pair straddles the antimeridian.
            if (Math.Abs(lon2 - lon1) > Math.PI)
                lon1 += 2 * Math.PI;

            var lat3 = (lat1 + lat2) / 2;

            var f1 = Math.Tan((Math.PI / 4) + (lat1 / 2));
            var f2 = Math.Tan((Math.PI / 4) + (lat2 / 2));
            var f3 = Math.Tan((Math.PI / 4) + (lat3 / 2));

            var denominator = Math.Log(f2 / f1);

            var lon3 = Math.Abs(denominator) > 1e-12
                ? (((lon2 - lon1) * Math.Log(f3)) + (lon1 * Math.Log(f2)) - (lon2 * Math.Log(f1))) / denominator
                : (lon1 + lon2) / 2;   // parallel of latitude: plain average

            return GeodesyInternals.PointFromRadians(lat3, lon3);
        }

        /// <summary>
        /// The inverse Gudermannian-style stretch used by the Mercator projection, guarded
        /// against the infinity at the poles.
        /// </summary>
        private static double MercatorStretch(double latitudeRadians)
        {
            var clamped = MathCompat.Clamp(latitudeRadians, -(Math.PI / 2) + 1e-12, (Math.PI / 2) - 1e-12);
            return Math.Log(Math.Tan((Math.PI / 4) + (clamped / 2)));
        }
    }
}
