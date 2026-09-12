using System.Globalization;
using GeoCore.Compatibility;
using GeoCore.Core;
using GeoCore.Formats;
using GeoCore.Geodesy;
using GeoCore.Units;
using GeoCore.Units.Conversions;

namespace GeoCore.Extensions
{
    /// <summary>
    /// Geospatial calculations for <see cref="GeoPoint"/>.
    /// </summary>
    /// <remarks>
    /// Unless noted otherwise these use spherical (great-circle) maths, which is accurate to
    /// roughly 0.5%. Where sub-metre accuracy matters, use
    /// <see cref="GeodesicDistanceTo"/> or the <see cref="Vincenty"/> class directly.
    /// </remarks>
    public static class GeoPointExtensions
    {
        /// <summary>
        /// Calculates the great-circle distance between two geographic points.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <param name="unit">The unit of distance (default is kilometres).</param>
        /// <returns>The distance between the two points in the specified unit.</returns>
        public static double DistanceTo(this GeoPoint from, GeoPoint to,
            DistanceUnit unit = DistanceUnit.Kilometers) =>
            DistanceConverter.FromMeters(Spherical.DistanceMeters(from, to), unit);

        /// <summary>
        /// Calculates the distance between two points along a geodesic on the WGS-84
        /// ellipsoid, using Vincenty's inverse formula.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <param name="unit">The unit of distance (default is kilometres).</param>
        /// <returns>The distance between the two points in the specified unit.</returns>
        /// <exception cref="GeodesyConvergenceException">
        /// The points are very nearly antipodal, where Vincenty's formula does not converge.
        /// Use <see cref="TryGeodesicDistanceTo"/> or <see cref="DistanceTo"/> instead.
        /// </exception>
        /// <remarks>
        /// This is accurate to within about half a millimetre, at the cost of an iterative
        /// solve. <see cref="DistanceTo"/> is cheaper and usually good enough.
        /// </remarks>
        public static double GeodesicDistanceTo(this GeoPoint from, GeoPoint to,
            DistanceUnit unit = DistanceUnit.Kilometers) =>
            DistanceConverter.FromMeters(Vincenty.Inverse(from, to).DistanceMeters, unit);

        /// <summary>
        /// Attempts to calculate the ellipsoidal geodesic distance between two points.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <param name="distance">Receives the distance, or 0 if the calculation did not converge.</param>
        /// <param name="unit">The unit of distance (default is kilometres).</param>
        /// <returns><c>true</c> if a solution was found; otherwise <c>false</c>.</returns>
        public static bool TryGeodesicDistanceTo(this GeoPoint from, GeoPoint to,
            out double distance, DistanceUnit unit = DistanceUnit.Kilometers)
        {
            if (Vincenty.TryInverse(from, to, out var segment))
            {
                distance = DistanceConverter.FromMeters(segment.DistanceMeters, unit);
                return true;
            }

            distance = 0;
            return false;
        }

        /// <summary>
        /// Calculates the initial bearing (forward azimuth) from one point to another.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <returns>The bearing in degrees clockwise from true north, in [0, 360).</returns>
        public static double BearingTo(this GeoPoint from, GeoPoint to) =>
            Spherical.InitialBearingDegrees(from, to);

        /// <summary>
        /// Calculates the bearing on arrival at <paramref name="to"/> along the great circle
        /// from <paramref name="from"/>.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <returns>The bearing in degrees clockwise from true north, in [0, 360).</returns>
        /// <remarks>
        /// A great circle is not a constant-bearing path, so this generally differs from
        /// <see cref="BearingTo"/> — dramatically so over long distances at high latitudes.
        /// </remarks>
        public static double FinalBearingTo(this GeoPoint from, GeoPoint to) =>
            Spherical.FinalBearingDegrees(from, to);

        /// <summary>
        /// Returns the half-way point along the great circle between two points.
        /// </summary>
        /// <param name="from">The first point.</param>
        /// <param name="to">The second point.</param>
        /// <returns>The midpoint.</returns>
        public static GeoPoint MidpointTo(this GeoPoint from, GeoPoint to) =>
            Spherical.Midpoint(from, to);

        /// <summary>
        /// Returns the point a given fraction of the way along the great circle to another
        /// point.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <param name="fraction">The fraction of the way along the arc; 0 is <paramref name="from"/> and 1 is <paramref name="to"/>.</param>
        /// <returns>The interpolated point.</returns>
        public static GeoPoint IntermediatePointTo(this GeoPoint from, GeoPoint to, double fraction) =>
            Spherical.IntermediatePoint(from, to, fraction);

        /// <summary>
        /// Moves from a starting point along a great circle in the specified direction and
        /// distance.
        /// </summary>
        /// <param name="point">The starting point.</param>
        /// <param name="distance">The distance to move. Negative values travel in the opposite direction.</param>
        /// <param name="bearingDegrees">The initial bearing, in degrees clockwise from true north.</param>
        /// <param name="unit">The distance unit (default: kilometres).</param>
        /// <returns>A new point at the destination location.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="distance"/> is not finite.</exception>
        public static GeoPoint Move(this GeoPoint point, double distance, double bearingDegrees,
            DistanceUnit unit = DistanceUnit.Kilometers)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance))
                throw new ArgumentOutOfRangeException(nameof(distance), distance,
                    "Distance must be a finite number.");

            return Spherical.Destination(point, DistanceConverter.ToMeters(distance, unit), bearingDegrees);
        }

        /// <summary>
        /// Moves from a starting point along a geodesic on the WGS-84 ellipsoid, using
        /// Vincenty's direct formula.
        /// </summary>
        /// <param name="point">The starting point.</param>
        /// <param name="distance">The distance to move.</param>
        /// <param name="bearingDegrees">The initial bearing, in degrees clockwise from true north.</param>
        /// <param name="unit">The distance unit (default: kilometres).</param>
        /// <returns>A new point at the destination location.</returns>
        public static GeoPoint GeodesicMove(this GeoPoint point, double distance, double bearingDegrees,
            DistanceUnit unit = DistanceUnit.Kilometers) =>
            Vincenty.Direct(point, DistanceConverter.ToMeters(distance, unit), bearingDegrees).Destination;

        /// <summary>
        /// Calculates the distance along a rhumb line (a path of constant bearing) to another
        /// point.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <param name="unit">The unit of distance (default is kilometres).</param>
        /// <returns>The rhumb-line distance in the specified unit.</returns>
        public static double RhumbDistanceTo(this GeoPoint from, GeoPoint to,
            DistanceUnit unit = DistanceUnit.Kilometers) =>
            DistanceConverter.FromMeters(Spherical.RhumbDistanceMeters(from, to), unit);

        /// <summary>
        /// Calculates the constant bearing of the rhumb line to another point.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <returns>The bearing in degrees clockwise from true north, in [0, 360).</returns>
        public static double RhumbBearingTo(this GeoPoint from, GeoPoint to) =>
            Spherical.RhumbBearingDegrees(from, to);

        /// <summary>
        /// Moves from a starting point along a constant bearing.
        /// </summary>
        /// <param name="point">The starting point.</param>
        /// <param name="distance">The distance to move.</param>
        /// <param name="bearingDegrees">The constant bearing, in degrees clockwise from true north.</param>
        /// <param name="unit">The distance unit (default: kilometres).</param>
        /// <returns>A new point at the destination location.</returns>
        public static GeoPoint RhumbMove(this GeoPoint point, double distance, double bearingDegrees,
            DistanceUnit unit = DistanceUnit.Kilometers) =>
            Spherical.RhumbDestination(point, DistanceConverter.ToMeters(distance, unit), bearingDegrees);

        /// <summary>
        /// Returns the half-way point along the rhumb line to another point.
        /// </summary>
        /// <param name="from">The first point.</param>
        /// <param name="to">The second point.</param>
        /// <returns>The rhumb midpoint.</returns>
        public static GeoPoint RhumbMidpointTo(this GeoPoint from, GeoPoint to) =>
            Spherical.RhumbMidpoint(from, to);

        /// <summary>
        /// Returns how far this point lies to the side of the great circle through two other
        /// points.
        /// </summary>
        /// <param name="point">The point to measure.</param>
        /// <param name="pathStart">The start of the path.</param>
        /// <param name="pathEnd">The end of the path.</param>
        /// <param name="unit">The unit of distance (default is kilometres).</param>
        /// <returns>
        /// The perpendicular distance. Positive values lie to the left of the path, negative
        /// values to the right.
        /// </returns>
        /// <remarks>
        /// The path is an infinite great circle, not a bounded segment. Use
        /// <see cref="DistanceToSegment"/> if you want the distance to the segment itself.
        /// </remarks>
        public static double CrossTrackDistanceTo(this GeoPoint point, GeoPoint pathStart, GeoPoint pathEnd,
            DistanceUnit unit = DistanceUnit.Kilometers) =>
            DistanceConverter.FromMeters(
                Spherical.CrossTrackDistanceMeters(point, pathStart, pathEnd), unit);

        /// <summary>
        /// Returns how far along a path this point's closest approach lies.
        /// </summary>
        /// <param name="point">The point to measure.</param>
        /// <param name="pathStart">The start of the path.</param>
        /// <param name="pathEnd">The end of the path.</param>
        /// <param name="unit">The unit of distance (default is kilometres).</param>
        /// <returns>
        /// The distance from <paramref name="pathStart"/> to the foot of the perpendicular,
        /// negative if that point lies behind the start.
        /// </returns>
        public static double AlongTrackDistanceTo(this GeoPoint point, GeoPoint pathStart, GeoPoint pathEnd,
            DistanceUnit unit = DistanceUnit.Kilometers) =>
            DistanceConverter.FromMeters(
                Spherical.AlongTrackDistanceMeters(point, pathStart, pathEnd), unit);

        /// <summary>
        /// Returns the closest point to this one on the bounded segment between two points.
        /// </summary>
        /// <param name="point">The point to measure from.</param>
        /// <param name="segmentStart">The start of the segment.</param>
        /// <param name="segmentEnd">The end of the segment.</param>
        /// <returns>
        /// The nearest point on the segment, which is one of the endpoints when the
        /// perpendicular falls outside it.
        /// </returns>
        public static GeoPoint ClosestPointOnSegment(this GeoPoint point, GeoPoint segmentStart, GeoPoint segmentEnd)
        {
            if (segmentStart == segmentEnd)
                return segmentStart;

            var alongTrackMeters = Spherical.AlongTrackDistanceMeters(point, segmentStart, segmentEnd);

            if (alongTrackMeters <= 0)
                return segmentStart;

            var segmentLengthMeters = Spherical.DistanceMeters(segmentStart, segmentEnd);
            if (alongTrackMeters >= segmentLengthMeters)
                return segmentEnd;

            return Spherical.Destination(
                segmentStart, alongTrackMeters, Spherical.InitialBearingDegrees(segmentStart, segmentEnd));
        }

        /// <summary>
        /// Returns the shortest distance from this point to the bounded segment between two
        /// points.
        /// </summary>
        /// <param name="point">The point to measure from.</param>
        /// <param name="segmentStart">The start of the segment.</param>
        /// <param name="segmentEnd">The end of the segment.</param>
        /// <param name="unit">The unit of distance (default is kilometres).</param>
        /// <returns>The distance to the nearest point on the segment.</returns>
        public static double DistanceToSegment(this GeoPoint point, GeoPoint segmentStart, GeoPoint segmentEnd,
            DistanceUnit unit = DistanceUnit.Kilometers) =>
            point.DistanceTo(point.ClosestPointOnSegment(segmentStart, segmentEnd), unit);

        /// <summary>
        /// Determines whether another point lies within a given distance of this one.
        /// </summary>
        /// <param name="point">The centre point.</param>
        /// <param name="other">The point to test.</param>
        /// <param name="distance">The radius to test against.</param>
        /// <param name="unit">The unit of <paramref name="distance"/> (default is kilometres).</param>
        /// <returns><c>true</c> if <paramref name="other"/> is within <paramref name="distance"/>.</returns>
        public static bool IsWithin(this GeoPoint point, GeoPoint other, double distance,
            DistanceUnit unit = DistanceUnit.Kilometers) =>
            Spherical.DistanceMeters(point, other) <= DistanceConverter.ToMeters(distance, unit);

        /// <summary>
        /// Returns the smallest bounding box that fully contains a circle of the given radius
        /// around this point.
        /// </summary>
        /// <param name="point">The centre of the box.</param>
        /// <param name="radius">The radius of the circle to enclose.</param>
        /// <param name="unit">The unit of the radius (default is kilometres).</param>
        /// <returns>A bounding box surrounding the circle.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is negative, NaN or infinite.</exception>
        /// <remarks>
        /// <para>
        /// The returned box is a true bound on a sphere: the longitude span widens correctly
        /// towards the poles, and if the circle encloses a pole the box spans every longitude.
        /// </para>
        /// <para>
        /// Near the antimeridian the box may wrap, in which case
        /// <see cref="GeoBoundingBox.CrossesAntimeridian"/> is <c>true</c> and
        /// <see cref="GeoBoundingBox.MinLongitude"/> is greater than
        /// <see cref="GeoBoundingBox.MaxLongitude"/>. <see cref="GeoBoundingBox.Contains(GeoPoint)"/>
        /// handles that correctly.
        /// </para>
        /// </remarks>
        public static GeoBoundingBox GetBoundingBox(this GeoPoint point, double radius,
            DistanceUnit unit = DistanceUnit.Kilometers)
        {
            if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius), radius,
                    "Radius must be a non-negative, finite number.");

            var radiusMeters = DistanceConverter.ToMeters(radius, unit);
            var angularRadius = radiusMeters / GeoConstants.EarthRadiusMeters;

            var latRad = AngleConverter.DegreesToRadians(point.Latitude);
            var lonRad = AngleConverter.DegreesToRadians(point.Longitude);

            var minLatRad = latRad - angularRadius;
            var maxLatRad = latRad + angularRadius;

            // If the circle swallows a pole, every meridian is inside it.
            if (minLatRad <= -Math.PI / 2 || maxLatRad >= Math.PI / 2)
            {
                return new GeoBoundingBox(
                    MinLatitude: AngleConverter.RadiansToDegrees(Math.Max(minLatRad, -Math.PI / 2)),
                    MinLongitude: GeoConstants.MinLongitude,
                    MaxLatitude: AngleConverter.RadiansToDegrees(Math.Min(maxLatRad, Math.PI / 2)),
                    MaxLongitude: GeoConstants.MaxLongitude);
            }

            // The widest point of the circle sits on the parallel nearest the pole.
            var latitudeDelta = Math.Asin(MathCompat.Clamp(
                Math.Sin(angularRadius) / Math.Cos(latRad), -1.0, 1.0));

            return new GeoBoundingBox(
                MinLatitude: AngleConverter.RadiansToDegrees(minLatRad),
                MinLongitude: AngleConverter.NormalizeLongitude(
                    AngleConverter.RadiansToDegrees(lonRad - latitudeDelta)),
                MaxLatitude: AngleConverter.RadiansToDegrees(maxLatRad),
                MaxLongitude: AngleConverter.NormalizeLongitude(
                    AngleConverter.RadiansToDegrees(lonRad + latitudeDelta)));
        }

        /// <summary>
        /// Converts the point to a string in DMS (degrees, minutes, seconds) format with
        /// N/S/E/W suffixes.
        /// </summary>
        /// <param name="point">The point to format.</param>
        /// <param name="secondsDecimals">The number of decimal places on the seconds (default 2).</param>
        /// <returns>A formatted string such as <c>51°30'26.64"N, 0°07'40.08"W</c>.</returns>
        /// <remarks>
        /// Formatting always uses the invariant culture, so the output does not change with
        /// the ambient locale.
        /// </remarks>
        public static string ToDmsString(this GeoPoint point, int secondsDecimals = 2) =>
            CoordinateFormatter.ToDms(
                point.Latitude, point.Longitude, secondsDecimals, CultureInfo.InvariantCulture);

        /// <summary>
        /// Converts the point to a string in degrees and decimal minutes, the convention used
        /// by most marine and aviation equipment.
        /// </summary>
        /// <param name="point">The point to format.</param>
        /// <param name="minuteDecimals">The number of decimal places on the minutes (default 4).</param>
        /// <returns>A formatted string such as <c>51°30.4440'N, 0°07.6680'W</c>.</returns>
        public static string ToDegreesDecimalMinutesString(this GeoPoint point, int minuteDecimals = 4) =>
            CoordinateFormatter.ToDegreesDecimalMinutes(
                point.Latitude, point.Longitude, minuteDecimals, CultureInfo.InvariantCulture);
    }
}
