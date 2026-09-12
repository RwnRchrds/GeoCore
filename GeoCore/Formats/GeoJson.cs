using System.Text.Json;
using GeoCore.Core;

namespace GeoCore.Formats
{
    /// <summary>
    /// Reads and writes GeoJSON geometries (RFC 7946).
    /// </summary>
    /// <remarks>
    /// <para>
    /// GeoJSON lists coordinates as <c>[longitude, latitude]</c> — the opposite order to how
    /// coordinates are usually spoken, and the single most common source of bugs when working
    /// with the format.
    /// </para>
    /// <para>
    /// The readers accept either a bare geometry or a <c>Feature</c> wrapping one, so JSON
    /// straight from a mapping API usually works without unwrapping it yourself.
    /// </para>
    /// </remarks>
    public static class GeoJson
    {
        /// <summary>
        /// Writes a point as a GeoJSON <c>Point</c> geometry.
        /// </summary>
        /// <param name="point">The point to write.</param>
        /// <param name="indented">Whether to pretty-print the output.</param>
        /// <returns>The GeoJSON text.</returns>
        public static string Write(GeoPoint point, bool indented = false) =>
            WriteJson(indented, writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("type", "Point");
                writer.WritePropertyName("coordinates");
                WriteCoordinate(writer, point);
                writer.WriteEndObject();
            });

        /// <summary>
        /// Writes a route as a GeoJSON <c>LineString</c> geometry.
        /// </summary>
        /// <param name="route">The route to write.</param>
        /// <param name="indented">Whether to pretty-print the output.</param>
        /// <returns>The GeoJSON text.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="route"/> is <c>null</c>.</exception>
        public static string Write(GeoRoute route, bool indented = false)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            return WriteJson(indented, writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("type", "LineString");
                writer.WritePropertyName("coordinates");
                WriteCoordinateArray(writer, route.Points);
                writer.WriteEndObject();
            });
        }

        /// <summary>
        /// Writes a polygon as a GeoJSON <c>Polygon</c> geometry, with the exterior ring first
        /// and any holes after it.
        /// </summary>
        /// <param name="polygon">The polygon to write.</param>
        /// <param name="indented">Whether to pretty-print the output.</param>
        /// <returns>The GeoJSON text.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="polygon"/> is <c>null</c>.</exception>
        /// <remarks>Rings are written closed, with the first position repeated at the end, as RFC 7946 requires.</remarks>
        public static string Write(GeoPolygon polygon, bool indented = false)
        {
            if (polygon is null)
                throw new ArgumentNullException(nameof(polygon));

            return WriteJson(indented, writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("type", "Polygon");
                writer.WritePropertyName("coordinates");
                writer.WriteStartArray();

                WriteRing(writer, polygon.Vertices);

                foreach (var hole in polygon.Holes)
                    WriteRing(writer, hole);

                writer.WriteEndArray();
                writer.WriteEndObject();
            });
        }

        /// <summary>
        /// Writes a bounding box as a GeoJSON <c>Polygon</c> geometry.
        /// </summary>
        /// <param name="box">The box to write.</param>
        /// <param name="indented">Whether to pretty-print the output.</param>
        /// <returns>The GeoJSON text.</returns>
        public static string Write(GeoBoundingBox box, bool indented = false) =>
            WriteJson(indented, writer =>
            {
                writer.WriteStartObject();
                writer.WriteString("type", "Polygon");
                writer.WritePropertyName("coordinates");
                writer.WriteStartArray();
                WriteRing(writer, new[] { box.SouthWest, box.SouthEast, box.NorthEast, box.NorthWest });
                writer.WriteEndArray();
                writer.WriteEndObject();
            });

        /// <summary>
        /// Reads a GeoJSON <c>Point</c>.
        /// </summary>
        /// <param name="json">The GeoJSON text, either a bare geometry or a <c>Feature</c>.</param>
        /// <returns>The parsed point.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The JSON is malformed or is not a <c>Point</c>.</exception>
        public static GeoPoint ReadPoint(string json)
        {
            using var document = ParseDocument(json);
            var geometry = UnwrapGeometry(document.RootElement);

            ExpectType(geometry, "Point");

            return ReadCoordinate(GetCoordinates(geometry));
        }

        /// <summary>
        /// Reads a GeoJSON <c>LineString</c> as a route.
        /// </summary>
        /// <param name="json">The GeoJSON text, either a bare geometry or a <c>Feature</c>.</param>
        /// <returns>The parsed route.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The JSON is malformed or is not a <c>LineString</c>.</exception>
        public static GeoRoute ReadRoute(string json)
        {
            using var document = ParseDocument(json);
            var geometry = UnwrapGeometry(document.RootElement);

            ExpectType(geometry, "LineString");

            var points = ReadCoordinateArray(GetCoordinates(geometry));

            if (points.Count < 2)
                throw new FormatException(
                    $"A LineString needs at least two positions to form a route, but found {points.Count}.");

            return new GeoRoute(points);
        }

        /// <summary>
        /// Reads a GeoJSON <c>Polygon</c>, treating the first ring as the exterior and the
        /// rest as holes.
        /// </summary>
        /// <param name="json">The GeoJSON text, either a bare geometry or a <c>Feature</c>.</param>
        /// <returns>The parsed polygon.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The JSON is malformed or is not a <c>Polygon</c>.</exception>
        public static GeoPolygon ReadPolygon(string json)
        {
            using var document = ParseDocument(json);
            var geometry = UnwrapGeometry(document.RootElement);

            ExpectType(geometry, "Polygon");

            var coordinates = GetCoordinates(geometry);

            if (coordinates.ValueKind != JsonValueKind.Array || coordinates.GetArrayLength() == 0)
                throw new FormatException("A Polygon must contain at least an exterior ring.");

            var rings = new List<List<GeoPoint>>();

            foreach (var ring in coordinates.EnumerateArray())
                rings.Add(ReadCoordinateArray(ring));

            var holes = rings.Skip(1).Select(r => (IEnumerable<GeoPoint>)r).ToList();

            return new GeoPolygon(rings[0], holes);
        }

        /// <summary>
        /// Attempts to read a GeoJSON <c>Point</c>.
        /// </summary>
        /// <param name="json">The GeoJSON text.</param>
        /// <param name="point">Receives the parsed point, or <c>default</c> on failure.</param>
        /// <returns><c>true</c> if parsing succeeded.</returns>
        public static bool TryReadPoint(string? json, out GeoPoint point)
        {
            point = default;

            if (json is null)
                return false;

            try
            {
                point = ReadPoint(json);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string WriteJson(bool indented, Action<Utf8JsonWriter> write)
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
            {
                write(writer);
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        private static void WriteCoordinate(Utf8JsonWriter writer, GeoPoint point)
        {
            // RFC 7946 positions are [longitude, latitude].
            writer.WriteStartArray();
            writer.WriteNumberValue(point.Longitude);
            writer.WriteNumberValue(point.Latitude);
            writer.WriteEndArray();
        }

        private static void WriteCoordinateArray(Utf8JsonWriter writer, IReadOnlyList<GeoPoint> points)
        {
            writer.WriteStartArray();

            foreach (var point in points)
                WriteCoordinate(writer, point);

            writer.WriteEndArray();
        }

        private static void WriteRing(Utf8JsonWriter writer, IReadOnlyList<GeoPoint> ring)
        {
            writer.WriteStartArray();

            foreach (var point in ring)
                WriteCoordinate(writer, point);

            // GeoJSON rings must be explicitly closed.
            if (ring.Count > 0 && ring[0] != ring[ring.Count - 1])
                WriteCoordinate(writer, ring[0]);

            writer.WriteEndArray();
        }

        private static JsonDocument ParseDocument(string json)
        {
            if (json is null)
                throw new ArgumentNullException(nameof(json));

            try
            {
                return JsonDocument.Parse(json);
            }
            catch (JsonException exception)
            {
                throw new FormatException("The GeoJSON text is not valid JSON.", exception);
            }
        }

        /// <summary>
        /// Returns the geometry itself, reaching inside a Feature wrapper if there is one.
        /// </summary>
        private static JsonElement UnwrapGeometry(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
                throw new FormatException("A GeoJSON geometry must be a JSON object.");

            if (element.TryGetProperty("type", out var type) &&
                type.ValueKind == JsonValueKind.String &&
                string.Equals(type.GetString(), "Feature", StringComparison.Ordinal))
            {
                if (!element.TryGetProperty("geometry", out var geometry) ||
                    geometry.ValueKind != JsonValueKind.Object)
                {
                    throw new FormatException("The GeoJSON Feature does not contain a geometry object.");
                }

                return geometry;
            }

            return element;
        }

        private static void ExpectType(JsonElement geometry, string expected)
        {
            if (!geometry.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String)
                throw new FormatException("The GeoJSON geometry is missing its \"type\" property.");

            var actual = type.GetString();

            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new FormatException($"Expected a GeoJSON {expected} but found {actual}.");
        }

        private static JsonElement GetCoordinates(JsonElement geometry)
        {
            if (!geometry.TryGetProperty("coordinates", out var coordinates))
                throw new FormatException("The GeoJSON geometry is missing its \"coordinates\" property.");

            return coordinates;
        }

        private static GeoPoint ReadCoordinate(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() < 2)
                throw new FormatException(
                    "A GeoJSON position must be an array of at least two numbers, [longitude, latitude].");

            double longitude;
            double latitude;

            try
            {
                longitude = element[0].GetDouble();
                latitude = element[1].GetDouble();
            }
            catch (InvalidOperationException exception)
            {
                throw new FormatException("A GeoJSON position must contain numbers.", exception);
            }

            if (!GeoPoint.IsValid(latitude, longitude))
                throw new FormatException(FormattableString.Invariant(
                    $"The position ({latitude}, {longitude}) is out of range. Remember GeoJSON writes longitude before latitude."));

            return new GeoPoint(latitude, longitude);
        }

        private static List<GeoPoint> ReadCoordinateArray(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Array)
                throw new FormatException("Expected an array of GeoJSON positions.");

            var points = new List<GeoPoint>(element.GetArrayLength());

            foreach (var item in element.EnumerateArray())
                points.Add(ReadCoordinate(item));

            return points;
        }
    }
}
