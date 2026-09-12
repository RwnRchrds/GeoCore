using System.Globalization;
using GeoCore.Compatibility;
using GeoCore.Units;
using GeoCore.Units.Conversions;

namespace GeoCore.Core
{
    /// <summary>
    /// Represents a rectangular region of the globe, bounded by a range of latitudes and a
    /// range of longitudes.
    /// </summary>
    /// <remarks>
    /// A box may wrap across the antimeridian, in which case <see cref="MinLongitude"/> is
    /// greater than <see cref="MaxLongitude"/> and <see cref="CrossesAntimeridian"/> is
    /// <c>true</c>. All the operations on this type account for that; comparing the raw
    /// longitudes yourself will not.
    /// </remarks>
    public readonly record struct GeoBoundingBox
    {
        private readonly double _minLatitude;
        private readonly double _minLongitude;
        private readonly double _maxLatitude;
        private readonly double _maxLongitude;

        /// <summary>
        /// Initializes a new <see cref="GeoBoundingBox"/>.
        /// </summary>
        /// <param name="MinLatitude">The southern edge, in degrees.</param>
        /// <param name="MinLongitude">The western edge, in degrees.</param>
        /// <param name="MaxLatitude">The northern edge, in degrees.</param>
        /// <param name="MaxLongitude">
        /// The eastern edge, in degrees. May be less than <paramref name="MinLongitude"/>, in
        /// which case the box wraps across the antimeridian.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A value is out of range or not finite, or <paramref name="MinLatitude"/> is greater
        /// than <paramref name="MaxLatitude"/>.
        /// </exception>
        public GeoBoundingBox(double MinLatitude, double MinLongitude, double MaxLatitude, double MaxLongitude)
        {
            _minLatitude = ValidateLatitude(MinLatitude, nameof(MinLatitude));
            _maxLatitude = ValidateLatitude(MaxLatitude, nameof(MaxLatitude));
            _minLongitude = ValidateLongitude(MinLongitude, nameof(MinLongitude));
            _maxLongitude = ValidateLongitude(MaxLongitude, nameof(MaxLongitude));

            if (_minLatitude > _maxLatitude)
                throw new ArgumentOutOfRangeException(nameof(MinLatitude), MinLatitude,
                    FormattableString.Invariant(
                        $"The southern edge ({MinLatitude}) must not be north of the northern edge ({MaxLatitude})."));
        }

        /// <summary>Gets the southern edge of the box, in degrees.</summary>
        public double MinLatitude { get => _minLatitude; init => _minLatitude = ValidateLatitude(value, nameof(value)); }

        /// <summary>Gets the western edge of the box, in degrees.</summary>
        public double MinLongitude { get => _minLongitude; init => _minLongitude = ValidateLongitude(value, nameof(value)); }

        /// <summary>Gets the northern edge of the box, in degrees.</summary>
        public double MaxLatitude { get => _maxLatitude; init => _maxLatitude = ValidateLatitude(value, nameof(value)); }

        /// <summary>Gets the eastern edge of the box, in degrees.</summary>
        public double MaxLongitude { get => _maxLongitude; init => _maxLongitude = ValidateLongitude(value, nameof(value)); }

        /// <summary>
        /// Deconstructs the box into its four edges.
        /// </summary>
        /// <param name="minLatitude">Receives the southern edge.</param>
        /// <param name="minLongitude">Receives the western edge.</param>
        /// <param name="maxLatitude">Receives the northern edge.</param>
        /// <param name="maxLongitude">Receives the eastern edge.</param>
        public void Deconstruct(out double minLatitude, out double minLongitude,
            out double maxLatitude, out double maxLongitude)
        {
            minLatitude = _minLatitude;
            minLongitude = _minLongitude;
            maxLatitude = _maxLatitude;
            maxLongitude = _maxLongitude;
        }

        /// <summary>
        /// A box covering the entire globe.
        /// </summary>
        public static GeoBoundingBox World => new(-90, -180, 90, 180);

        /// <summary>
        /// Gets a value indicating whether the box wraps across the antimeridian (the 180°
        /// meridian).
        /// </summary>
        public bool CrossesAntimeridian => _minLongitude > _maxLongitude;

        /// <summary>Gets the south-west corner of the box.</summary>
        public GeoPoint SouthWest => new(_minLatitude, _minLongitude);

        /// <summary>Gets the north-east corner of the box.</summary>
        public GeoPoint NorthEast => new(_maxLatitude, _maxLongitude);

        /// <summary>Gets the north-west corner of the box.</summary>
        public GeoPoint NorthWest => new(_maxLatitude, _minLongitude);

        /// <summary>Gets the south-east corner of the box.</summary>
        public GeoPoint SouthEast => new(_minLatitude, _maxLongitude);

        /// <summary>
        /// Gets the height of the box in degrees of latitude.
        /// </summary>
        public double LatitudeSpan => _maxLatitude - _minLatitude;

        /// <summary>
        /// Gets the width of the box in degrees of longitude, accounting for a box that wraps
        /// across the antimeridian.
        /// </summary>
        public double LongitudeSpan
        {
            get
            {
                var span = _maxLongitude - _minLongitude;
                return span < 0 ? span + 360.0 : span;
            }
        }

        /// <summary>
        /// Gets the point at the centre of the box.
        /// </summary>
        public GeoPoint Center => new(
            (_minLatitude + _maxLatitude) / 2.0,
            AngleConverter.NormalizeLongitude(_minLongitude + (LongitudeSpan / 2.0)));

        /// <summary>
        /// Gets a value indicating whether the box has no extent in either direction.
        /// </summary>
        public bool IsEmpty => LatitudeSpan == 0 && LongitudeSpan == 0;

        /// <summary>
        /// Checks whether a given point falls within the box. Edges are inclusive.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns><c>true</c> if the point lies inside or on the boundary of the box.</returns>
        public bool Contains(GeoPoint point)
        {
            if (point.Latitude < _minLatitude || point.Latitude > _maxLatitude)
                return false;

            return ContainsLongitude(point.Longitude);
        }

        /// <summary>
        /// Checks whether a longitude falls within the box's longitude range.
        /// </summary>
        /// <param name="longitude">The longitude in degrees.</param>
        /// <returns><c>true</c> if the longitude lies within the box's east-west extent.</returns>
        public bool ContainsLongitude(double longitude)
        {
            var lon = AngleConverter.NormalizeLongitude(longitude);

            // A wrapped box covers everything east of Min *or* west of Max.
            if (CrossesAntimeridian)
                return lon >= _minLongitude || lon <= _maxLongitude;

            return lon >= _minLongitude && lon <= _maxLongitude;
        }

        /// <summary>
        /// Checks whether another box lies entirely within this one.
        /// </summary>
        /// <param name="other">The box to test.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is wholly contained.</returns>
        public bool Contains(GeoBoundingBox other)
        {
            if (other._minLatitude < _minLatitude || other._maxLatitude > _maxLatitude)
                return false;

            if (LongitudeSpan >= 360.0)
                return true;

            if (other.LongitudeSpan > LongitudeSpan)
                return false;

            // Both edges inside, and the other box's span short enough not to wrap past us.
            return ContainsLongitude(other._minLongitude) && ContainsLongitude(other._maxLongitude);
        }

        /// <summary>
        /// Checks whether this box shares any area with another.
        /// </summary>
        /// <param name="other">The box to test.</param>
        /// <returns><c>true</c> if the boxes overlap or touch.</returns>
        public bool Intersects(GeoBoundingBox other)
        {
            if (other._minLatitude > _maxLatitude || other._maxLatitude < _minLatitude)
                return false;

            return LongitudesOverlap(this, other);
        }

        /// <summary>
        /// Returns the smallest box containing both this box and another.
        /// </summary>
        /// <param name="other">The box to merge with.</param>
        /// <returns>The union of the two boxes.</returns>
        /// <remarks>
        /// East-west, the result is the narrower of the two ways of joining the boxes around
        /// the globe, so unioning a box either side of the antimeridian produces a narrow
        /// wrapped box rather than one spanning almost the whole world.
        /// </remarks>
        public GeoBoundingBox Union(GeoBoundingBox other)
        {
            var minLatitude = Math.Min(_minLatitude, other._minLatitude);
            var maxLatitude = Math.Max(_maxLatitude, other._maxLatitude);

            var (minLongitude, span) = UnionLongitudes(this, other);

            if (span >= 360.0)
                return new GeoBoundingBox(minLatitude, -180, maxLatitude, 180);

            return new GeoBoundingBox(
                minLatitude,
                minLongitude,
                maxLatitude,
                AngleConverter.NormalizeLongitude(minLongitude + span));
        }

        /// <summary>
        /// Returns a box grown by the given distance on all four sides.
        /// </summary>
        /// <param name="distance">The distance to grow by. Must not be negative.</param>
        /// <param name="unit">The unit of <paramref name="distance"/> (default is kilometres).</param>
        /// <returns>The expanded box.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="distance"/> is negative, NaN or infinite.</exception>
        /// <remarks>
        /// The longitude growth is computed at whichever edge is nearer a pole, so the result
        /// is a true bound. If growing the box reaches a pole, it widens to every longitude.
        /// </remarks>
        public GeoBoundingBox Expand(double distance, DistanceUnit unit = DistanceUnit.Kilometers)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0)
                throw new ArgumentOutOfRangeException(nameof(distance), distance,
                    "Distance must be a non-negative, finite number.");

            var angularDistance = DistanceConverter.ToMeters(distance, unit) / GeoConstants.EarthRadiusMeters;
            var deltaLatitude = AngleConverter.RadiansToDegrees(angularDistance);

            var minLatitude = _minLatitude - deltaLatitude;
            var maxLatitude = _maxLatitude + deltaLatitude;

            if (minLatitude <= GeoConstants.MinLatitude || maxLatitude >= GeoConstants.MaxLatitude)
            {
                return new GeoBoundingBox(
                    Math.Max(minLatitude, GeoConstants.MinLatitude), -180,
                    Math.Min(maxLatitude, GeoConstants.MaxLatitude), 180);
            }

            // A degree of longitude is shortest at the edge closest to a pole, so that edge
            // dictates how far we must widen.
            var worstLatitude = Math.Max(Math.Abs(minLatitude), Math.Abs(maxLatitude));
            var cosLatitude = Math.Cos(AngleConverter.DegreesToRadians(worstLatitude));

            if (cosLatitude < 1e-12)
                return new GeoBoundingBox(minLatitude, -180, maxLatitude, 180);

            var deltaLongitude = AngleConverter.RadiansToDegrees(
                Math.Asin(MathCompat.Clamp(Math.Sin(angularDistance) / cosLatitude, -1.0, 1.0)));

            if (LongitudeSpan + (2 * deltaLongitude) >= 360.0)
                return new GeoBoundingBox(minLatitude, -180, maxLatitude, 180);

            return new GeoBoundingBox(
                minLatitude,
                AngleConverter.NormalizeLongitude(_minLongitude - deltaLongitude),
                maxLatitude,
                AngleConverter.NormalizeLongitude(_maxLongitude + deltaLongitude));
        }

        /// <summary>
        /// Returns the approximate area of the box on the Earth's surface.
        /// </summary>
        /// <param name="unit">The unit of the resulting area (default: square kilometres).</param>
        /// <returns>The area of the box.</returns>
        public double Area(AreaUnit unit = AreaUnit.SquareKilometers)
        {
            // The area of a latitude band is R² · Δλ · (sin φ₂ − sin φ₁).
            var deltaLongitude = AngleConverter.DegreesToRadians(LongitudeSpan);
            var sinMax = Math.Sin(AngleConverter.DegreesToRadians(_maxLatitude));
            var sinMin = Math.Sin(AngleConverter.DegreesToRadians(_minLatitude));

            var squareMeters = GeoConstants.EarthRadiusMeters * GeoConstants.EarthRadiusMeters *
                               deltaLongitude * (sinMax - sinMin);

            return AreaConverter.FromSquareMeters(squareMeters, unit);
        }

        /// <summary>
        /// Returns the smallest box containing all the given points.
        /// </summary>
        /// <param name="points">The points to enclose.</param>
        /// <returns>The bounding box of the points.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="points"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="points"/> is empty.</exception>
        /// <remarks>
        /// East-west, this finds the widest empty gap between consecutive longitudes and
        /// excludes it, so a cluster of points either side of the antimeridian yields a narrow
        /// wrapped box rather than one spanning nearly the whole globe.
        /// </remarks>
        public static GeoBoundingBox FromPoints(IEnumerable<GeoPoint> points)
        {
            if (points is null)
                throw new ArgumentNullException(nameof(points));

            var list = points as IList<GeoPoint> ?? points.ToList();

            if (list.Count == 0)
                throw new ArgumentException("At least one point is required.", nameof(points));

            var minLatitude = double.MaxValue;
            var maxLatitude = double.MinValue;
            var longitudes = new double[list.Count];

            for (var i = 0; i < list.Count; i++)
            {
                var point = list[i];
                if (point.Latitude < minLatitude) minLatitude = point.Latitude;
                if (point.Latitude > maxLatitude) maxLatitude = point.Latitude;
                longitudes[i] = point.Longitude;
            }

            Array.Sort(longitudes);

            // Find the widest gap between neighbouring longitudes, treating the list as a
            // circle. The bounding arc is everything except that gap.
            var widestGap = double.MinValue;
            var gapStartIndex = 0;

            for (var i = 0; i < longitudes.Length; i++)
            {
                var next = (i + 1) % longitudes.Length;
                var gap = longitudes[next] - longitudes[i];
                if (gap < 0)
                    gap += 360.0;

                if (gap > widestGap)
                {
                    widestGap = gap;
                    gapStartIndex = i;
                }
            }

            var minLongitude = longitudes[(gapStartIndex + 1) % longitudes.Length];
            var maxLongitude = longitudes[gapStartIndex];

            return new GeoBoundingBox(minLatitude, minLongitude, maxLatitude, maxLongitude);
        }

        /// <summary>
        /// Returns the smallest box containing all the given points.
        /// </summary>
        /// <param name="points">The points to enclose.</param>
        /// <returns>The bounding box of the points.</returns>
        public static GeoBoundingBox FromPoints(params GeoPoint[] points) =>
            FromPoints((IEnumerable<GeoPoint>)points);

        /// <inheritdoc />
        public override string ToString() => string.Format(
            CultureInfo.InvariantCulture,
            "BoundingBox(Lat: {0:F6} to {1:F6}, Lon: {2:F6} to {3:F6})",
            _minLatitude, _maxLatitude, _minLongitude, _maxLongitude);

        private static bool LongitudesOverlap(GeoBoundingBox a, GeoBoundingBox b)
        {
            if (a.LongitudeSpan >= 360.0 || b.LongitudeSpan >= 360.0)
                return true;

            // Two arcs on a circle overlap unless each one's start lies in the other's gap.
            return a.ContainsLongitude(b._minLongitude) ||
                   a.ContainsLongitude(b._maxLongitude) ||
                   b.ContainsLongitude(a._minLongitude) ||
                   b.ContainsLongitude(a._maxLongitude);
        }

        private static (double Start, double Span) UnionLongitudes(GeoBoundingBox a, GeoBoundingBox b)
        {
            if (a.LongitudeSpan >= 360.0 || b.LongitudeSpan >= 360.0)
                return (-180.0, 360.0);

            var fromA = SpanFrom(a._minLongitude, a, b);
            var fromB = SpanFrom(b._minLongitude, a, b);

            return fromA <= fromB ? (a._minLongitude, fromA) : (b._minLongitude, fromB);
        }

        /// <summary>
        /// Returns the arc length needed, starting at <paramref name="start"/> and running
        /// east, to cover both boxes.
        /// </summary>
        private static double SpanFrom(double start, GeoBoundingBox a, GeoBoundingBox b)
        {
            var needed = Math.Max(OffsetEast(start, a._minLongitude) + a.LongitudeSpan,
                                  OffsetEast(start, b._minLongitude) + b.LongitudeSpan);

            return Math.Min(needed, 360.0);
        }

        private static double OffsetEast(double from, double to)
        {
            var offset = (to - from) % 360.0;
            return offset < 0 ? offset + 360.0 : offset;
        }

        private static double ValidateLatitude(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(paramName, value, "Latitude must be a finite number.");

            if (value < GeoConstants.MinLatitude || value > GeoConstants.MaxLatitude)
                throw new ArgumentOutOfRangeException(paramName, value,
                    "Latitude must be between -90 and 90 degrees.");

            return value;
        }

        private static double ValidateLongitude(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(paramName, value, "Longitude must be a finite number.");

            if (value < GeoConstants.MinLongitude || value > GeoConstants.MaxLongitude)
                throw new ArgumentOutOfRangeException(paramName, value,
                    "Longitude must be between -180 and 180 degrees.");

            return value;
        }
    }
}
