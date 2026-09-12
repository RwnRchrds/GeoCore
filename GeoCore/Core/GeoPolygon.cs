using System.Collections.ObjectModel;
using System.Globalization;
using GeoCore.Extensions;
using GeoCore.Geodesy;
using GeoCore.Units;
using GeoCore.Units.Conversions;

namespace GeoCore.Core
{
    /// <summary>
    /// Represents a closed polygon on the Earth's surface, defined by an ordered ring of
    /// vertices and optionally by interior rings that punch holes in it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Edges are treated as straight lines in latitude/longitude space rather than as great
    /// circles. That is the convention GeoJSON and most mapping tools use, and for polygons
    /// spanning less than a few hundred kilometres the difference is negligible.
    /// </para>
    /// <para>
    /// Polygons that wrap across the antimeridian are handled, provided the polygon spans
    /// less than 180° of longitude. A polygon wider than that is ambiguous and will not be
    /// interpreted as you expect.
    /// </para>
    /// </remarks>
    public class GeoPolygon
    {
        private readonly GeoPoint[] _vertices;
        private readonly GeoPoint[][] _holes;
        private readonly Ring _exteriorRing;
        private readonly Ring[] _holeRings;

        /// <summary>
        /// Initializes a new polygon from an exterior ring and optional interior rings.
        /// </summary>
        /// <param name="vertices">
        /// The ordered vertices of the exterior ring. A repeated closing vertex is optional
        /// and is removed if present. At least three distinct vertices are required.
        /// </param>
        /// <param name="holes">
        /// Optional interior rings. Each must also have at least three distinct vertices.
        /// Winding order is irrelevant; any ring listed here is treated as a hole.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="vertices"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">A ring has fewer than three distinct vertices.</exception>
        public GeoPolygon(IEnumerable<GeoPoint> vertices, IEnumerable<IEnumerable<GeoPoint>>? holes = null)
        {
            if (vertices is null)
                throw new ArgumentNullException(nameof(vertices));

            _vertices = NormalizeRing(vertices, nameof(vertices));

            if (holes is null)
            {
                _holes = Array.Empty<GeoPoint[]>();
            }
            else
            {
                var holeList = new List<GeoPoint[]>();
                foreach (var hole in holes)
                {
                    if (hole is null)
                        throw new ArgumentException("A hole ring must not be null.", nameof(holes));

                    holeList.Add(NormalizeRing(hole, nameof(holes)));
                }

                _holes = holeList.ToArray();
            }

            _exteriorRing = Ring.Create(_vertices);
            _holeRings = _holes.Select(Ring.Create).ToArray();

            Vertices = new ReadOnlyCollection<GeoPoint>(_vertices);
            Holes = new ReadOnlyCollection<IReadOnlyList<GeoPoint>>(
                _holes.Select(h => (IReadOnlyList<GeoPoint>)new ReadOnlyCollection<GeoPoint>(h)).ToList());

            BoundingBox = GeoBoundingBox.FromPoints(_vertices);
        }

        /// <summary>
        /// Gets the vertices of the exterior ring, in order and without a repeated closing
        /// vertex.
        /// </summary>
        public IReadOnlyList<GeoPoint> Vertices { get; }

        /// <summary>
        /// Gets the interior rings that are excluded from the polygon, each without a repeated
        /// closing vertex.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<GeoPoint>> Holes { get; }

        /// <summary>
        /// Gets the smallest bounding box containing the exterior ring.
        /// </summary>
        public GeoBoundingBox BoundingBox { get; }

        /// <summary>
        /// Gets a value indicating whether the exterior ring is wound clockwise when viewed on
        /// a conventional north-up map.
        /// </summary>
        public bool IsClockwise => SignedPlanarArea(_exteriorRing) < 0;

        /// <summary>
        /// Determines whether the specified point lies inside the polygon.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns><c>true</c> if the point is inside the exterior ring and not inside a hole.</returns>
        /// <remarks>
        /// <para>
        /// Uses even-odd ray casting with a half-open edge rule, so the result never depends
        /// on which vertex the ring happens to start at.
        /// </para>
        /// <para>
        /// Points lying exactly on the boundary are a genuine edge case for any ray-casting
        /// test: the answer is deterministic but is not guaranteed to be <c>true</c>. Use
        /// <see cref="IsOnBoundary"/> if boundary membership matters to you.
        /// </para>
        /// </remarks>
        public bool Contains(GeoPoint point)
        {
            if (!RingContains(_exteriorRing, point))
                return false;

            foreach (var hole in _holeRings)
            {
                if (RingContains(hole, point))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether a point lies on the polygon's boundary, within a tolerance.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="tolerance">How close to an edge the point must be to count as on it.</param>
        /// <param name="unit">The unit of <paramref name="tolerance"/> (default is metres).</param>
        /// <returns><c>true</c> if the point lies within <paramref name="tolerance"/> of any edge.</returns>
        public bool IsOnBoundary(GeoPoint point, double tolerance = 1.0,
            DistanceUnit unit = DistanceUnit.Meters)
        {
            var toleranceMeters = DistanceConverter.ToMeters(tolerance, unit);

            if (RingDistanceMeters(_vertices, point) <= toleranceMeters)
                return true;

            foreach (var hole in _holes)
            {
                if (RingDistanceMeters(hole, point) <= toleranceMeters)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the area of the polygon on the Earth's surface using spherical excess,
        /// with the area of any holes subtracted.
        /// </summary>
        /// <param name="unit">The unit of the resulting area (default: square kilometres).</param>
        /// <returns>The area of the polygon.</returns>
        /// <remarks>
        /// Longitude steps are wrapped into ±180°, so a polygon straddling the antimeridian
        /// measures correctly rather than wrapping the long way round the globe.
        /// </remarks>
        public double Area(AreaUnit unit = AreaUnit.SquareKilometers)
        {
            var squareMeters = RingAreaSquareMeters(_vertices);

            foreach (var hole in _holes)
                squareMeters -= RingAreaSquareMeters(hole);

            if (squareMeters < 0)
                squareMeters = 0;

            return AreaConverter.FromSquareMeters(squareMeters, unit);
        }

        /// <summary>
        /// Calculates the length of the polygon's exterior ring.
        /// </summary>
        /// <param name="unit">The unit of the result (default is kilometres).</param>
        /// <returns>The perimeter of the exterior ring.</returns>
        /// <remarks>Holes do not contribute; use <see cref="TotalBoundaryLength"/> for that.</remarks>
        public double Perimeter(DistanceUnit unit = DistanceUnit.Kilometers) =>
            DistanceConverter.FromMeters(RingLengthMeters(_vertices), unit);

        /// <summary>
        /// Calculates the combined length of the exterior ring and every hole.
        /// </summary>
        /// <param name="unit">The unit of the result (default is kilometres).</param>
        /// <returns>The total boundary length.</returns>
        public double TotalBoundaryLength(DistanceUnit unit = DistanceUnit.Kilometers)
        {
            var meters = RingLengthMeters(_vertices);

            foreach (var hole in _holes)
                meters += RingLengthMeters(hole);

            return DistanceConverter.FromMeters(meters, unit);
        }

        /// <summary>
        /// Returns the area-weighted centroid of the exterior ring.
        /// </summary>
        /// <returns>The centroid of the polygon.</returns>
        /// <remarks>
        /// This is the centroid of the ring projected into latitude/longitude space. For a
        /// degenerate ring with no area it falls back to the mean of the vertices. Note that
        /// the centroid of a concave polygon may lie outside it.
        /// </remarks>
        public GeoPoint Centroid()
        {
            var longitudes = _exteriorRing.Longitudes;
            var signedArea = 0.0;
            var latitudeSum = 0.0;
            var longitudeSum = 0.0;

            for (var i = 0; i < _vertices.Length; i++)
            {
                var nextIndex = (i + 1) % _vertices.Length;
                var current = _vertices[i];
                var next = _vertices[nextIndex];

                var x1 = longitudes[i];
                var x2 = longitudes[nextIndex];
                var y1 = current.Latitude;
                var y2 = next.Latitude;

                var cross = (x1 * y2) - (x2 * y1);
                signedArea += cross;
                longitudeSum += (x1 + x2) * cross;
                latitudeSum += (y1 + y2) * cross;
            }

            if (Math.Abs(signedArea) < 1e-14)
            {
                // A zero-area ring (collinear or repeated points) has no meaningful centroid.
                return GeoPoint.Clamped(
                    _vertices.Average(v => v.Latitude), longitudes.Average());
            }

            var factor = 1.0 / (3.0 * signedArea);

            return GeoPoint.Clamped(latitudeSum * factor, longitudeSum * factor);
        }

        /// <summary>
        /// Returns a copy of the polygon with its exterior ring simplified, removing vertices
        /// that stray less than <paramref name="tolerance"/> from the simplified outline.
        /// </summary>
        /// <param name="tolerance">The maximum deviation to allow.</param>
        /// <param name="unit">The unit of <paramref name="tolerance"/> (default is metres).</param>
        /// <returns>A simplified polygon, or the same polygon if it cannot be reduced further.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="tolerance"/> is negative, NaN or infinite.</exception>
        /// <remarks>
        /// Holes are simplified with the same tolerance. A ring that would be reduced below
        /// three vertices is left as it is, so the result is always a valid polygon.
        /// </remarks>
        public GeoPolygon Simplify(double tolerance, DistanceUnit unit = DistanceUnit.Meters)
        {
            if (double.IsNaN(tolerance) || double.IsInfinity(tolerance) || tolerance < 0)
                throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance,
                    "Tolerance must be a non-negative, finite number.");

            var toleranceMeters = DistanceConverter.ToMeters(tolerance, unit);

            var exterior = SimplifyRing(_vertices, toleranceMeters);
            var holes = _holes.Select(h => (IEnumerable<GeoPoint>)SimplifyRing(h, toleranceMeters)).ToList();

            return new GeoPolygon(exterior, holes);
        }

        /// <inheritdoc />
        public override string ToString() => string.Format(
            CultureInfo.InvariantCulture,
            "GeoPolygon(Vertices: {0}, Holes: {1}, BoundingBox: {2})",
            _vertices.Length, _holes.Length, BoundingBox);

        /// <summary>
        /// Even-odd ray casting with a half-open edge rule.
        /// </summary>
        /// <remarks>
        /// Each edge counts only if the test latitude falls in its half-open latitude span,
        /// so a vertex is never counted twice and the outcome does not depend on where the
        /// ring starts. The ring's longitudes are unwrapped into a continuous run when the
        /// polygon is built, and the test point is projected into that same frame, which is
        /// what keeps the test correct for a ring straddling the antimeridian.
        /// </remarks>
        private static bool RingContains(Ring ring, GeoPoint point)
        {
            var points = ring.Points;
            var longitudes = ring.Longitudes;

            var testLatitude = point.Latitude;
            var testLongitude = ring.Project(point.Longitude);

            var inside = false;

            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                var currentLatitude = points[i].Latitude;
                var previousLatitude = points[j].Latitude;

                // Half-open span: strictly one endpoint above the ray, one at or below it.
                if (currentLatitude > testLatitude == previousLatitude > testLatitude)
                    continue;

                var fraction = (testLatitude - currentLatitude) / (previousLatitude - currentLatitude);
                var crossingLongitude =
                    longitudes[i] + (fraction * (longitudes[j] - longitudes[i]));

                // The ray runs east from the test point.
                if (crossingLongitude > testLongitude)
                    inside = !inside;
            }

            return inside;
        }

        private static double RingAreaSquareMeters(GeoPoint[] ring)
        {
            var total = 0.0;

            for (var i = 0; i < ring.Length; i++)
            {
                var current = ring[i];
                var next = ring[(i + 1) % ring.Length];

                // Wrapping the step keeps a ring that straddles the antimeridian from being
                // measured the long way around the globe.
                var deltaLongitude = AngleConverter.DegreesToRadians(
                    AngleConverter.NormalizeLongitude(next.Longitude - current.Longitude));

                total += deltaLongitude *
                         (2 + Math.Sin(AngleConverter.DegreesToRadians(current.Latitude)) +
                          Math.Sin(AngleConverter.DegreesToRadians(next.Latitude)));
            }

            return Math.Abs(total) * GeoConstants.EarthRadiusMeters * GeoConstants.EarthRadiusMeters / 2.0;
        }

        private static double RingLengthMeters(GeoPoint[] ring)
        {
            var total = 0.0;

            for (var i = 0; i < ring.Length; i++)
                total += Spherical.DistanceMeters(ring[i], ring[(i + 1) % ring.Length]);

            return total;
        }

        private static double RingDistanceMeters(GeoPoint[] ring, GeoPoint point)
        {
            var shortest = double.MaxValue;

            for (var i = 0; i < ring.Length; i++)
            {
                var distance = point.DistanceToSegment(
                    ring[i], ring[(i + 1) % ring.Length], DistanceUnit.Meters);

                if (distance < shortest)
                    shortest = distance;
            }

            return shortest;
        }

        private static double SignedPlanarArea(Ring ring)
        {
            var points = ring.Points;
            var longitudes = ring.Longitudes;
            var total = 0.0;

            for (var i = 0; i < points.Length; i++)
            {
                var next = (i + 1) % points.Length;

                total += (longitudes[next] - longitudes[i]) *
                         (points[next].Latitude + points[i].Latitude);
            }

            // Negated so that a positive result means counter-clockwise on a north-up map.
            return -total / 2.0;
        }

        private static List<GeoPoint> SimplifyRing(GeoPoint[] ring, double toleranceMeters)
        {
            // Close the ring first so the algorithm can also drop the starting vertex's
            // neighbours, then re-open it.
            var closed = new List<GeoPoint>(ring.Length + 1);
            closed.AddRange(ring);
            closed.Add(ring[0]);

            var simplified = Simplification.DouglasPeucker(closed, toleranceMeters);

            if (simplified.Count > 1 && simplified[0] == simplified[simplified.Count - 1])
                simplified.RemoveAt(simplified.Count - 1);

            return simplified.Count >= 3 ? simplified : new List<GeoPoint>(ring);
        }

        /// <summary>
        /// A ring together with its longitudes unwrapped into a continuous run, so that a
        /// ring crossing the antimeridian reads as (for example) 179°, 181° rather than
        /// jumping from 179° to -179°.
        /// </summary>
        private sealed class Ring
        {
            private Ring(GeoPoint[] points, double[] longitudes)
            {
                Points = points;
                Longitudes = longitudes;
            }

            internal GeoPoint[] Points { get; }

            internal double[] Longitudes { get; }

            internal static Ring Create(GeoPoint[] points)
            {
                var longitudes = new double[points.Length];
                longitudes[0] = points[0].Longitude;

                // Each step is the *shortest* way round from the previous vertex, accumulated
                // so the ring never jumps by 360 part-way through.
                for (var i = 1; i < points.Length; i++)
                {
                    longitudes[i] = longitudes[i - 1] + AngleConverter.NormalizeLongitude(
                        points[i].Longitude - points[i - 1].Longitude);
                }

                return new Ring(points, longitudes);
            }

            /// <summary>
            /// Places an arbitrary longitude into this ring's unwrapped frame.
            /// </summary>
            internal double Project(double longitude) =>
                Longitudes[0] + AngleConverter.NormalizeLongitude(longitude - Points[0].Longitude);
        }

        private static GeoPoint[] NormalizeRing(IEnumerable<GeoPoint> ring, string paramName)
        {
            var list = ring.ToList();

            // A repeated closing vertex is conventional in GeoJSON but redundant here.
            while (list.Count > 1 && list[0] == list[list.Count - 1])
                list.RemoveAt(list.Count - 1);

            if (list.Count < 3)
                throw new ArgumentException(
                    "A polygon ring must have at least 3 distinct points.", paramName);

            return list.ToArray();
        }
    }
}
