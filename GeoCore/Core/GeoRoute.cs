using System.Collections.ObjectModel;
using System.Globalization;
using GeoCore.Extensions;
using GeoCore.Geodesy;
using GeoCore.Units;
using GeoCore.Units.Conversions;

namespace GeoCore.Core
{
    /// <summary>
    /// Describes where a point projects onto a route.
    /// </summary>
    /// <param name="Point">The closest point on the route.</param>
    /// <param name="SegmentIndex">The zero-based index of the segment the point falls on.</param>
    /// <param name="DistanceAlongRoute">How far along the route the closest point lies.</param>
    /// <param name="DistanceFromRoute">How far the original point is from the route.</param>
    public readonly record struct GeoRoutePosition(
        GeoPoint Point,
        int SegmentIndex,
        double DistanceAlongRoute,
        double DistanceFromRoute);

    /// <summary>
    /// Represents a sequential collection of <see cref="GeoPoint"/>s forming a geospatial route.
    /// </summary>
    /// <remarks>
    /// Segment lengths are computed once on first use and cached, so repeatedly querying
    /// positions along a route does not re-measure it.
    /// </remarks>
    public class GeoRoute
    {
        private readonly GeoPoint[] _points;
        private double[]? _cumulativeMeters;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoRoute"/> class.
        /// </summary>
        /// <param name="points">The sequence of points that make up the route. Must contain at least two points.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="points"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if fewer than two points are provided.</exception>
        public GeoRoute(IEnumerable<GeoPoint> points)
        {
            if (points is null)
                throw new ArgumentNullException(nameof(points));

            _points = points.ToArray();

            if (_points.Length < 2)
                throw new ArgumentException("A route must contain at least two points.", nameof(points));

            Points = new ReadOnlyCollection<GeoPoint>(_points);
        }

        /// <summary>
        /// Gets the ordered list of points in the route.
        /// </summary>
        public IReadOnlyList<GeoPoint> Points { get; }

        /// <summary>
        /// Gets the starting point of the route.
        /// </summary>
        public GeoPoint Start => _points[0];

        /// <summary>
        /// Gets the ending point of the route.
        /// </summary>
        public GeoPoint End => _points[_points.Length - 1];

        /// <summary>
        /// Gets the number of segments in the route, one fewer than the number of points.
        /// </summary>
        public int SegmentCount => _points.Length - 1;

        /// <summary>
        /// Gets the smallest bounding box containing every point on the route.
        /// </summary>
        public GeoBoundingBox BoundingBox => GeoBoundingBox.FromPoints(_points);

        /// <summary>
        /// Enumerates the route's segments as start/end pairs.
        /// </summary>
        /// <returns>One pair per segment, in route order.</returns>
        public IEnumerable<(GeoPoint Start, GeoPoint End)> Segments()
        {
            for (var i = 0; i < _points.Length - 1; i++)
                yield return (_points[i], _points[i + 1]);
        }

        /// <summary>
        /// Calculates the total distance of the route by summing the distances between each
        /// consecutive point.
        /// </summary>
        /// <param name="unit">The unit of distance to return (default is kilometres).</param>
        /// <returns>The total distance of the route in the specified unit.</returns>
        public double TotalDistance(DistanceUnit unit = DistanceUnit.Kilometers) =>
            DistanceConverter.FromMeters(TotalMeters, unit);

        /// <summary>
        /// Returns the distance from the start of the route to each point on it.
        /// </summary>
        /// <param name="unit">The unit of distance to return (default is kilometres).</param>
        /// <returns>
        /// One value per point, starting at zero and ending at the route's total distance.
        /// </returns>
        public IReadOnlyList<double> CumulativeDistances(DistanceUnit unit = DistanceUnit.Kilometers)
        {
            var cumulative = Cumulative;
            var result = new double[cumulative.Length];

            for (var i = 0; i < cumulative.Length; i++)
                result[i] = DistanceConverter.FromMeters(cumulative[i], unit);

            return result;
        }

        /// <summary>
        /// Returns the initial bearing (forward azimuth) from each point to the next along the
        /// route.
        /// </summary>
        /// <returns>A list of bearings in degrees (0°–360°), one for each segment.</returns>
        public List<double> BearingsBetweenPoints()
        {
            var bearings = new List<double>(_points.Length - 1);

            for (var i = 0; i < _points.Length - 1; i++)
                bearings.Add(Spherical.InitialBearingDegrees(_points[i], _points[i + 1]));

            return bearings;
        }

        /// <summary>
        /// Returns the geographic point located at a specified distance along the route.
        /// </summary>
        /// <param name="distance">The distance to travel along the route from the starting point.</param>
        /// <param name="unit">The unit of distance (default is kilometres).</param>
        /// <returns>
        /// A <see cref="GeoPoint"/> located at the specified distance along the route.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="distance"/> is NaN or infinite.</exception>
        /// <remarks>
        /// The distance is clamped to the route: a negative distance returns
        /// <see cref="Start"/> and a distance beyond the route's length returns
        /// <see cref="End"/>, rather than extrapolating past either end.
        /// </remarks>
        public GeoPoint MoveAlongRoute(double distance, DistanceUnit unit = DistanceUnit.Kilometers)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance))
                throw new ArgumentOutOfRangeException(nameof(distance), distance,
                    "Distance must be a finite number.");

            var targetMeters = DistanceConverter.ToMeters(distance, unit);
            var cumulative = Cumulative;

            if (targetMeters <= 0)
                return Start;

            if (targetMeters >= cumulative[cumulative.Length - 1])
                return End;

            var index = FindSegment(cumulative, targetMeters);

            var segmentStart = _points[index];
            var segmentEnd = _points[index + 1];
            var intoSegment = targetMeters - cumulative[index];
            var segmentLength = cumulative[index + 1] - cumulative[index];

            if (segmentLength <= 0)
                return segmentStart;

            return Spherical.IntermediatePoint(segmentStart, segmentEnd, intoSegment / segmentLength);
        }

        /// <summary>
        /// Returns the point located at a fractional distance along the route (e.g. 0.5 = halfway).
        /// </summary>
        /// <param name="fraction">The fraction along the route (0.0 to 1.0).</param>
        /// <returns>The <see cref="GeoPoint"/> at the given fraction along the route.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="fraction"/> is outside 0.0–1.0.</exception>
        public GeoPoint MoveAlongRouteByFraction(double fraction)
        {
            if (double.IsNaN(fraction) || fraction < 0 || fraction > 1)
                throw new ArgumentOutOfRangeException(nameof(fraction), fraction,
                    "Fraction must be between 0.0 and 1.0");

            return MoveAlongRoute(fraction * TotalMeters, DistanceUnit.Meters);
        }

        /// <summary>
        /// Returns a list of evenly spaced points along the route, including the start and end
        /// points.
        /// </summary>
        /// <param name="count">The total number of points to generate (must be at least 2).</param>
        /// <returns>A list of interpolated <see cref="GeoPoint"/>s along the route.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than 2.</exception>
        public List<GeoPoint> InterpolatePoints(int count)
        {
            if (count < 2)
                throw new ArgumentOutOfRangeException(nameof(count), count,
                    "Must request at least two points.");

            var totalMeters = TotalMeters;
            var step = totalMeters / (count - 1);
            var points = new List<GeoPoint>(count);

            for (var i = 0; i < count; i++)
                points.Add(MoveAlongRoute(step * i, DistanceUnit.Meters));

            return points;
        }

        /// <summary>
        /// Finds the point on the route closest to the given location.
        /// </summary>
        /// <param name="point">The location to project onto the route.</param>
        /// <param name="unit">The unit for the distances in the result (default is kilometres).</param>
        /// <returns>The closest point, which segment it lies on, and how far along and away it is.</returns>
        public GeoRoutePosition ClosestPointTo(GeoPoint point, DistanceUnit unit = DistanceUnit.Kilometers)
        {
            var cumulative = Cumulative;

            var bestDistance = double.MaxValue;
            var bestPoint = Start;
            var bestIndex = 0;
            var bestAlong = 0.0;

            for (var i = 0; i < _points.Length - 1; i++)
            {
                var candidate = point.ClosestPointOnSegment(_points[i], _points[i + 1]);
                var distance = Spherical.DistanceMeters(point, candidate);

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestPoint = candidate;
                bestIndex = i;
                bestAlong = cumulative[i] + Spherical.DistanceMeters(_points[i], candidate);
            }

            return new GeoRoutePosition(
                bestPoint,
                bestIndex,
                DistanceConverter.FromMeters(bestAlong, unit),
                DistanceConverter.FromMeters(bestDistance, unit));
        }

        /// <summary>
        /// Returns the shortest distance from a point to the route.
        /// </summary>
        /// <param name="point">The point to measure from.</param>
        /// <param name="unit">The unit of distance (default is kilometres).</param>
        /// <returns>The distance to the nearest point on the route.</returns>
        public double DistanceTo(GeoPoint point, DistanceUnit unit = DistanceUnit.Kilometers) =>
            ClosestPointTo(point, unit).DistanceFromRoute;

        /// <summary>
        /// Returns the route travelled in the opposite direction.
        /// </summary>
        /// <returns>A new route with the points reversed.</returns>
        public GeoRoute Reversed() => new(Enumerable.Reverse(_points));

        /// <summary>
        /// Returns a copy of the route with redundant points removed.
        /// </summary>
        /// <param name="tolerance">The maximum deviation from the original path to allow.</param>
        /// <param name="unit">The unit of <paramref name="tolerance"/> (default is metres).</param>
        /// <returns>A simplified route that still starts and ends at the same points.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="tolerance"/> is negative, NaN or infinite.</exception>
        /// <remarks>
        /// Uses the Ramer-Douglas-Peucker algorithm, which is the usual way to thin a GPS
        /// track without losing its shape.
        /// </remarks>
        public GeoRoute Simplify(double tolerance, DistanceUnit unit = DistanceUnit.Meters)
        {
            if (double.IsNaN(tolerance) || double.IsInfinity(tolerance) || tolerance < 0)
                throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance,
                    "Tolerance must be a non-negative, finite number.");

            var simplified = Simplification.DouglasPeucker(
                _points, DistanceConverter.ToMeters(tolerance, unit));

            return new GeoRoute(simplified);
        }

        /// <inheritdoc />
        public override string ToString() => string.Format(
            CultureInfo.InvariantCulture,
            "GeoRoute(Points: {0}, Distance: {1:F3} km)", _points.Length, TotalDistance());

        private double TotalMeters
        {
            get
            {
                var cumulative = Cumulative;
                return cumulative[cumulative.Length - 1];
            }
        }

        private double[] Cumulative
        {
            get
            {
                // Measuring a long track is not free, so do it once and keep it.
                var cumulative = _cumulativeMeters;
                if (cumulative is not null)
                    return cumulative;

                cumulative = new double[_points.Length];
                var running = 0.0;

                for (var i = 1; i < _points.Length; i++)
                {
                    running += Spherical.DistanceMeters(_points[i - 1], _points[i]);
                    cumulative[i] = running;
                }

                _cumulativeMeters = cumulative;
                return cumulative;
            }
        }

        /// <summary>
        /// Returns the index of the segment containing the given distance along the route.
        /// </summary>
        private static int FindSegment(double[] cumulative, double targetMeters)
        {
            var index = Array.BinarySearch(cumulative, targetMeters);

            if (index >= 0)
                return Math.Min(index, cumulative.Length - 2);

            // BinarySearch returns the bitwise complement of the next-larger index.
            return Math.Min(Math.Max(~index - 1, 0), cumulative.Length - 2);
        }
    }
}
