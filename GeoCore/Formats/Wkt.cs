using System.Globalization;
using System.Text;
using GeoCore.Core;

namespace GeoCore.Formats
{
    /// <summary>
    /// Reads and writes geometries in Well-Known Text, the textual geometry format used by
    /// PostGIS, SQL Server spatial types and the OGC simple-features standard.
    /// </summary>
    /// <remarks>
    /// WKT lists coordinates as <c>x y</c>, that is <c>longitude latitude</c> — the opposite
    /// order to how coordinates are usually spoken. A third Z ordinate, if present, is read
    /// and discarded.
    /// </remarks>
    public static class Wkt
    {
        private const string NumberFormat = "0.###########";

        /// <summary>
        /// Writes a point as <c>POINT (lon lat)</c>.
        /// </summary>
        /// <param name="point">The point to write.</param>
        /// <returns>The WKT representation.</returns>
        public static string Write(GeoPoint point) =>
            "POINT (" + FormatCoordinate(point) + ")";

        /// <summary>
        /// Writes a route as a <c>LINESTRING</c>.
        /// </summary>
        /// <param name="route">The route to write.</param>
        /// <returns>The WKT representation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="route"/> is <c>null</c>.</exception>
        public static string Write(GeoRoute route)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            var builder = new StringBuilder("LINESTRING (");
            AppendCoordinates(builder, route.Points);
            builder.Append(')');

            return builder.ToString();
        }

        /// <summary>
        /// Writes a polygon as a <c>POLYGON</c>, with the exterior ring first and any holes
        /// after it.
        /// </summary>
        /// <param name="polygon">The polygon to write.</param>
        /// <returns>The WKT representation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="polygon"/> is <c>null</c>.</exception>
        /// <remarks>Rings are written closed, with the first vertex repeated at the end, as WKT requires.</remarks>
        public static string Write(GeoPolygon polygon)
        {
            if (polygon is null)
                throw new ArgumentNullException(nameof(polygon));

            var builder = new StringBuilder("POLYGON (");
            AppendRing(builder, polygon.Vertices);

            foreach (var hole in polygon.Holes)
            {
                builder.Append(", ");
                AppendRing(builder, hole);
            }

            builder.Append(')');

            return builder.ToString();
        }

        /// <summary>
        /// Writes a bounding box as a five-vertex <c>POLYGON</c>.
        /// </summary>
        /// <param name="box">The box to write.</param>
        /// <returns>The WKT representation.</returns>
        /// <remarks>
        /// A box that crosses the antimeridian cannot be expressed as a single WKT rectangle,
        /// so its eastern edge is written as its raw value; split the box yourself if the
        /// consumer needs valid geometry.
        /// </remarks>
        public static string Write(GeoBoundingBox box)
        {
            var builder = new StringBuilder("POLYGON (");

            AppendRing(builder, new[]
            {
                box.SouthWest, box.SouthEast, box.NorthEast, box.NorthWest
            });

            builder.Append(')');

            return builder.ToString();
        }

        /// <summary>
        /// Reads a <c>POINT</c> geometry.
        /// </summary>
        /// <param name="wkt">The WKT text.</param>
        /// <returns>The parsed point.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="wkt"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The text is not a valid <c>POINT</c>.</exception>
        public static GeoPoint ReadPoint(string wkt)
        {
            var index = 0;
            var text = Require(wkt);

            ExpectTag(text, ref index, "POINT");
            var points = ReadCoordinateList(text, ref index);
            ExpectEnd(text, index);

            if (points.Count != 1)
                throw new FormatException($"A POINT must contain exactly one coordinate, but found {points.Count}.");

            return points[0];
        }

        /// <summary>
        /// Reads a <c>LINESTRING</c> geometry as a route.
        /// </summary>
        /// <param name="wkt">The WKT text.</param>
        /// <returns>The parsed route.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="wkt"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The text is not a valid <c>LINESTRING</c>.</exception>
        public static GeoRoute ReadRoute(string wkt)
        {
            var index = 0;
            var text = Require(wkt);

            ExpectTag(text, ref index, "LINESTRING");
            var points = ReadCoordinateList(text, ref index);
            ExpectEnd(text, index);

            if (points.Count < 2)
                throw new FormatException(
                    $"A LINESTRING needs at least two coordinates to form a route, but found {points.Count}.");

            return new GeoRoute(points);
        }

        /// <summary>
        /// Reads a <c>POLYGON</c> geometry, treating the first ring as the exterior and the
        /// rest as holes.
        /// </summary>
        /// <param name="wkt">The WKT text.</param>
        /// <returns>The parsed polygon.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="wkt"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The text is not a valid <c>POLYGON</c>.</exception>
        public static GeoPolygon ReadPolygon(string wkt)
        {
            var index = 0;
            var text = Require(wkt);

            ExpectTag(text, ref index, "POLYGON");
            var rings = ReadRings(text, ref index);
            ExpectEnd(text, index);

            if (rings.Count == 0)
                throw new FormatException("A POLYGON must contain at least an exterior ring.");

            var holes = rings.Skip(1).Select(r => (IEnumerable<GeoPoint>)r).ToList();

            return new GeoPolygon(rings[0], holes);
        }

        /// <summary>
        /// Attempts to read a <c>POINT</c> geometry.
        /// </summary>
        /// <param name="wkt">The WKT text.</param>
        /// <param name="point">Receives the parsed point, or <c>default</c> on failure.</param>
        /// <returns><c>true</c> if parsing succeeded.</returns>
        public static bool TryReadPoint(string? wkt, out GeoPoint point)
        {
            point = default;

            if (wkt is null)
                return false;

            try
            {
                point = ReadPoint(wkt);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private static string Require(string wkt) =>
            wkt ?? throw new ArgumentNullException(nameof(wkt));

        private static string FormatCoordinate(GeoPoint point) =>
            // WKT is x-then-y, so longitude comes first.
            point.Longitude.ToString(NumberFormat, CultureInfo.InvariantCulture) + " " +
            point.Latitude.ToString(NumberFormat, CultureInfo.InvariantCulture);

        private static void AppendCoordinates(StringBuilder builder, IReadOnlyList<GeoPoint> points)
        {
            for (var i = 0; i < points.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(FormatCoordinate(points[i]));
            }
        }

        private static void AppendRing(StringBuilder builder, IReadOnlyList<GeoPoint> ring)
        {
            builder.Append('(');
            AppendCoordinates(builder, ring);

            // WKT rings must be explicitly closed.
            if (ring.Count > 0 && ring[0] != ring[ring.Count - 1])
            {
                builder.Append(", ");
                builder.Append(FormatCoordinate(ring[0]));
            }

            builder.Append(')');
        }

        private static void ExpectTag(string wkt, ref int index, string tag)
        {
            SkipWhitespace(wkt, ref index);

            var start = index;
            while (index < wkt.Length && char.IsLetter(wkt[index]))
                index++;

            var found = wkt.Substring(start, index - start).ToUpperInvariant();

            if (found != tag)
                throw new FormatException(
                    $"Expected a {tag} geometry but found '{(found.Length == 0 ? wkt.Trim() : found)}'.");

            SkipWhitespace(wkt, ref index);

            // "POINT Z (...)" and friends declare their dimensionality before the body.
            if (index < wkt.Length && (wkt[index] == 'Z' || wkt[index] == 'z' ||
                                       wkt[index] == 'M' || wkt[index] == 'm'))
            {
                while (index < wkt.Length && char.IsLetter(wkt[index]))
                    index++;

                SkipWhitespace(wkt, ref index);
            }
        }

        private static List<GeoPoint> ReadCoordinateList(string wkt, ref int index)
        {
            SkipWhitespace(wkt, ref index);
            Expect(wkt, ref index, '(');

            var points = new List<GeoPoint>();

            while (true)
            {
                SkipWhitespace(wkt, ref index);
                points.Add(ReadCoordinate(wkt, ref index));
                SkipWhitespace(wkt, ref index);

                if (index < wkt.Length && wkt[index] == ',')
                {
                    index++;
                    continue;
                }

                break;
            }

            Expect(wkt, ref index, ')');

            return points;
        }

        private static List<List<GeoPoint>> ReadRings(string wkt, ref int index)
        {
            SkipWhitespace(wkt, ref index);
            Expect(wkt, ref index, '(');

            var rings = new List<List<GeoPoint>>();

            while (true)
            {
                rings.Add(ReadCoordinateList(wkt, ref index));
                SkipWhitespace(wkt, ref index);

                if (index < wkt.Length && wkt[index] == ',')
                {
                    index++;
                    continue;
                }

                break;
            }

            Expect(wkt, ref index, ')');

            return rings;
        }

        private static GeoPoint ReadCoordinate(string wkt, ref int index)
        {
            var longitude = ReadNumber(wkt, ref index);
            SkipWhitespace(wkt, ref index);
            var latitude = ReadNumber(wkt, ref index);

            // Skip any Z (and M) ordinates: this library is two-dimensional.
            while (true)
            {
                var lookahead = index;
                SkipWhitespace(wkt, ref lookahead);

                if (lookahead >= wkt.Length ||
                    !(char.IsDigit(wkt[lookahead]) || wkt[lookahead] == '-' ||
                      wkt[lookahead] == '+' || wkt[lookahead] == '.'))
                {
                    break;
                }

                index = lookahead;
                ReadNumber(wkt, ref index);
            }

            if (!GeoPoint.IsValid(latitude, longitude))
                throw new FormatException(FormattableString.Invariant(
                    $"The coordinate ({latitude}, {longitude}) is out of range. Remember WKT writes longitude before latitude."));

            return new GeoPoint(latitude, longitude);
        }

        private static double ReadNumber(string wkt, ref int index)
        {
            SkipWhitespace(wkt, ref index);

            var start = index;

            if (index < wkt.Length && (wkt[index] == '-' || wkt[index] == '+'))
                index++;

            while (index < wkt.Length &&
                   (char.IsDigit(wkt[index]) || wkt[index] == '.' ||
                    wkt[index] == 'e' || wkt[index] == 'E' ||
                    ((wkt[index] == '-' || wkt[index] == '+') &&
                     (wkt[index - 1] == 'e' || wkt[index - 1] == 'E'))))
            {
                index++;
            }

            var text = wkt.Substring(start, index - start);

            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new FormatException(
                    $"Expected a number at position {start} but found '{(text.Length == 0 ? "end of input" : text)}'.");

            return value;
        }

        private static void Expect(string wkt, ref int index, char expected)
        {
            SkipWhitespace(wkt, ref index);

            if (index >= wkt.Length || wkt[index] != expected)
                throw new FormatException(
                    $"Expected '{expected}' at position {index} in the WKT geometry.");

            index++;
        }

        private static void ExpectEnd(string wkt, int index)
        {
            SkipWhitespace(wkt, ref index);

            if (index < wkt.Length)
                throw new FormatException(
                    $"Unexpected trailing content at position {index}: '{wkt.Substring(index)}'.");
        }

        private static void SkipWhitespace(string wkt, ref int index)
        {
            while (index < wkt.Length && char.IsWhiteSpace(wkt[index]))
                index++;
        }
    }
}
