using System.Globalization;
using GeoCore.Extensions;
using GeoCore.Geodesy;
using GeoCore.Units;
using GeoCore.Units.Conversions;

namespace GeoCore.Core
{
    /// <summary>
    /// Represents a circular region of the Earth's surface: every point within a given
    /// great-circle distance of a centre.
    /// </summary>
    /// <remarks>
    /// This is a spherical cap, not a circle on a flat map. Near the poles its projection
    /// onto a lat/lon grid is noticeably egg-shaped, which is why
    /// <see cref="BoundingBox"/> is worth using rather than estimating one yourself.
    /// </remarks>
    public readonly record struct GeoCircle
    {
        private readonly double _radiusMeters;

        /// <summary>
        /// Initializes a new circle.
        /// </summary>
        /// <param name="center">The centre of the circle.</param>
        /// <param name="radius">The radius, measured along the Earth's surface.</param>
        /// <param name="unit">The unit of <paramref name="radius"/> (default is kilometres).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is negative, NaN or infinite.</exception>
        public GeoCircle(GeoPoint center, double radius, DistanceUnit unit = DistanceUnit.Kilometers)
        {
            if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0)
                throw new ArgumentOutOfRangeException(nameof(radius), radius,
                    "Radius must be a non-negative, finite number.");

            Center = center;
            _radiusMeters = DistanceConverter.ToMeters(radius, unit);
        }

        /// <summary>
        /// Gets the centre of the circle.
        /// </summary>
        public GeoPoint Center { get; init; }

        /// <summary>
        /// Gets the radius of the circle in metres.
        /// </summary>
        public double RadiusMeters
        {
            get => _radiusMeters;
            init
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        "Radius must be a non-negative, finite number.");

                _radiusMeters = value;
            }
        }

        /// <summary>
        /// Gets the radius of the circle in the specified unit.
        /// </summary>
        /// <param name="unit">The unit to express the radius in.</param>
        /// <returns>The radius.</returns>
        public double Radius(DistanceUnit unit = DistanceUnit.Kilometers) =>
            DistanceConverter.FromMeters(_radiusMeters, unit);

        /// <summary>
        /// Gets the smallest bounding box fully containing the circle.
        /// </summary>
        public GeoBoundingBox BoundingBox => Center.GetBoundingBox(_radiusMeters, DistanceUnit.Meters);

        /// <summary>
        /// Determines whether a point lies inside the circle. The boundary counts as inside.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns><c>true</c> if the point is within the radius of the centre.</returns>
        public bool Contains(GeoPoint point) =>
            Spherical.DistanceMeters(Center, point) <= _radiusMeters;

        /// <summary>
        /// Determines whether another circle lies entirely within this one.
        /// </summary>
        /// <param name="other">The circle to test.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is wholly contained.</returns>
        public bool Contains(GeoCircle other) =>
            Spherical.DistanceMeters(Center, other.Center) + other._radiusMeters <= _radiusMeters;

        /// <summary>
        /// Determines whether this circle overlaps another.
        /// </summary>
        /// <param name="other">The circle to test.</param>
        /// <returns><c>true</c> if the circles overlap or touch.</returns>
        public bool Intersects(GeoCircle other) =>
            Spherical.DistanceMeters(Center, other.Center) <= _radiusMeters + other._radiusMeters;

        /// <summary>
        /// Returns the area enclosed by the circle.
        /// </summary>
        /// <param name="unit">The unit of the resulting area (default: square kilometres).</param>
        /// <returns>The area of the spherical cap.</returns>
        /// <remarks>
        /// Uses the spherical cap area 2πR²(1 − cos θ), which stays correct for very large
        /// radii where the flat πr² would not.
        /// </remarks>
        public double Area(AreaUnit unit = AreaUnit.SquareKilometers)
        {
            var angularRadius = _radiusMeters / GeoConstants.EarthRadiusMeters;

            var squareMeters = 2 * Math.PI * GeoConstants.EarthRadiusMeters *
                               GeoConstants.EarthRadiusMeters * (1 - Math.Cos(angularRadius));

            return AreaConverter.FromSquareMeters(squareMeters, unit);
        }

        /// <summary>
        /// Returns the length of the circle's boundary.
        /// </summary>
        /// <param name="unit">The unit of the result (default is kilometres).</param>
        /// <returns>The circumference of the circle on the sphere.</returns>
        /// <remarks>
        /// On a sphere this is 2πR·sin θ, which is shorter than the flat 2πr and shrinks back
        /// to zero as the circle grows to cover a hemisphere and beyond.
        /// </remarks>
        public double Circumference(DistanceUnit unit = DistanceUnit.Kilometers)
        {
            var angularRadius = _radiusMeters / GeoConstants.EarthRadiusMeters;

            return DistanceConverter.FromMeters(
                2 * Math.PI * GeoConstants.EarthRadiusMeters * Math.Sin(angularRadius), unit);
        }

        /// <summary>
        /// Approximates the circle as a polygon.
        /// </summary>
        /// <param name="segments">The number of vertices to generate. Must be at least 3.</param>
        /// <returns>A polygon inscribed in the circle.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="segments"/> is less than 3.</exception>
        /// <remarks>
        /// The vertices lie on the circle, so the polygon is slightly smaller than the circle
        /// itself. Use more segments if that matters.
        /// </remarks>
        public GeoPolygon ToPolygon(int segments = 64)
        {
            if (segments < 3)
                throw new ArgumentOutOfRangeException(nameof(segments), segments,
                    "A polygon needs at least 3 vertices.");

            var vertices = new List<GeoPoint>(segments);

            for (var i = 0; i < segments; i++)
            {
                var bearing = 360.0 * i / segments;
                vertices.Add(Spherical.Destination(Center, _radiusMeters, bearing));
            }

            return new GeoPolygon(vertices);
        }

        /// <inheritdoc />
        public override string ToString() =>
            // Call the point's formatter directly rather than relying on a "D" specifier
            // being dispatched through IFormattable at runtime.
            "GeoCircle(Center: " + Center.ToString("D", CultureInfo.InvariantCulture) +
            ", Radius: " + Radius().ToString("F3", CultureInfo.InvariantCulture) + " km)";
    }
}
