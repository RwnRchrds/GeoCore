using GeoCore.Extensions;
using GeoCore.Units;
using GeoCore.Units.Conversions;

namespace GeoCore.Core
{
    /// <summary>
    /// Represents a sequential collection of <see cref="GeoPoint"/>s forming a geospatial route.
    /// </summary>
    public class GeoRoute
    {
        /// <summary>
        /// Gets the ordered list of points in the route.
        /// </summary>
        public IReadOnlyList<GeoPoint> Points { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoRoute"/> class.
        /// </summary>
        /// <param name="points">The sequence of points that make up the route. Must contain at least two points.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="points"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if fewer than two points are provided.</exception>
        public GeoRoute(IEnumerable<GeoPoint> points)
        {
            var list = points?.ToList() ?? throw new ArgumentNullException(nameof(points));
            if (list.Count < 2)
                throw new ArgumentException("A route must contain at least two points.", nameof(points));

            Points = list.AsReadOnly();
        }

        /// <summary>
        /// Gets the starting point of the route.
        /// </summary>
        public GeoPoint Start => Points[0];

        /// <summary>
        /// Gets the ending point of the route.
        /// </summary>
        public GeoPoint End => Points[^1];

        /// <summary>
        /// Calculates the total distance of the route by summing the distances between each consecutive point.
        /// </summary>
        /// <param name="unit">The unit of distance to return (default is kilometers).</param>
        /// <returns>The total distance of the route in the specified unit.</returns>
        public double TotalDistance(DistanceUnit unit = DistanceUnit.Kilometers)
        {
            double total = 0;
            for (int i = 0; i < Points.Count - 1; i++)
            {
                total += Points[i].DistanceTo(Points[i + 1], unit);
            }

            return total;
        }

        /// <summary>
        /// Returns the initial bearing (forward azimuth) from each point to the next along the route.
        /// </summary>
        /// <returns>A list of bearings in degrees (0°–360°), one for each segment.</returns>
        public List<double> BearingsBetweenPoints()
        {
            var bearings = new List<double>();

            for (int i = 0; i < Points.Count - 1; i++)
            {
                var bearing = Points[i].BearingTo(Points[i + 1]);
                bearings.Add(bearing);
            }

            return bearings;
        }

        /// <summary>
        /// Returns the geographic point located at a specified distance along the route.
        /// </summary>
        /// <param name="distance">The distance to travel along the route from the starting point.</param>
        /// <param name="unit">The unit of distance (default is kilometers).</param>
        /// <returns>
        /// A <see cref="GeoPoint"/> located at the specified distance along the route.
        /// If the distance exceeds the total length of the route, the final point of the route is returned.
        /// </returns>
        public GeoPoint MoveAlongRoute(double distance, DistanceUnit unit = DistanceUnit.Kilometers)
        {
            var targetDistanceKm = DistanceConverter.ToKilometers(distance, unit);

            for (int i = 0; i < Points.Count - 1; i++)
            {
                var segmentStart = Points[i];
                var segmentEnd = Points[i + 1];
                var segmentDistanceKm = segmentStart.DistanceTo(segmentEnd, DistanceUnit.Kilometers);

                if (targetDistanceKm <= segmentDistanceKm)
                {
                    var bearing = segmentStart.BearingTo(segmentEnd);
                    return segmentStart.Move(targetDistanceKm, bearing, DistanceUnit.Kilometers);
                }

                targetDistanceKm -= segmentDistanceKm;
            }

            return Points[^1]; // If distance exceeds route length
        }

        /// <summary>
        /// Returns the point located at a fractional distance along the route (e.g. 0.5 = halfway).
        /// </summary>
        /// <param name="fraction">The fraction along the route (0.0 to 1.0).</param>
        /// <returns>The <see cref="GeoPoint"/> at the given fraction along the route.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="fraction"/> is outside 0.0–1.0.</exception>
        public GeoPoint MoveAlongRouteByFraction(double fraction)
        {
            if (fraction < 0 || fraction > 1)
                throw new ArgumentOutOfRangeException(nameof(fraction), "Fraction must be between 0.0 and 1.0");

            var totalKm = TotalDistance(DistanceUnit.Kilometers);
            return MoveAlongRoute(fraction * totalKm, DistanceUnit.Kilometers);
        }

        /// <summary>
        /// Returns a list of evenly spaced points along the route, including the start and end points.
        /// </summary>
        /// <param name="count">The total number of points to generate (must be at least 2).</param>
        /// <returns>A list of interpolated <see cref="GeoPoint"/>s along the route.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than 2.</exception>
        public List<GeoPoint> InterpolatePoints(int count)
        {
            if (count < 2)
                throw new ArgumentOutOfRangeException(nameof(count), "Must request at least two points.");

            var totalKm = TotalDistance(DistanceUnit.Kilometers);
            var stepKm = totalKm / (count - 1);
            var points = new List<GeoPoint>();

            for (int i = 0; i < count; i++)
            {
                var distance = stepKm * i;
                points.Add(MoveAlongRoute(distance, DistanceUnit.Kilometers));
            }

            return points;
        }
    }
}
