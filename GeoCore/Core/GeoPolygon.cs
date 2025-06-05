using GeoCore.Units.Conversions;
using System.Collections.ObjectModel;
using GeoCore.Units;

namespace GeoCore.Core
{
    /// <summary>
    /// Represents a closed polygon defined by an ordered list of GeoPoints.
    /// </summary>
    public class GeoPolygon
    {
        public IReadOnlyList<GeoPoint> Vertices { get; }
        public GeoBoundingBox BoundingBox { get; }

        public GeoPolygon(IEnumerable<GeoPoint> vertices)
        {
            var list = vertices.ToList();

            if (list.Count < 3)
                throw new ArgumentException("A polygon must have at least 3 points.", nameof(vertices));

            // Optional: ensure it's closed (first == last)
            if (list.First() != list.Last())
                list.Add(list.First());

            Vertices = new ReadOnlyCollection<GeoPoint>(list);
            BoundingBox = CalculateBoundingBox(Vertices);
        }

        private static GeoBoundingBox CalculateBoundingBox(IEnumerable<GeoPoint> points)
        {
            var minLat = points.Min(p => p.Latitude);
            var maxLat = points.Max(p => p.Latitude);
            var minLon = points.Min(p => p.Longitude);
            var maxLon = points.Max(p => p.Longitude);

            return new GeoBoundingBox(minLat, minLon, maxLat, maxLon);
        }

        public override string ToString() =>
            $"GeoPolygon(Vertices: {Vertices.Count - 1}, BoundingBox: {BoundingBox})";

        /// <summary>
        /// Determines if the specified point is inside the polygon using the ray casting algorithm.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the point is inside the polygon; otherwise, false.</returns>
        public bool Contains(GeoPoint point)
        {
            int crossings = 0;

            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                var a = Vertices[i];
                var b = Vertices[i + 1];

                // Ensure a.Latitude <= b.Latitude
                if (a.Latitude > b.Latitude)
                {
                    (a, b) = (b, a);
                }

                if (point.Latitude == a.Latitude || point.Latitude == b.Latitude)
                {
                    // Avoids ambiguity when point is exactly on a horizontal vertex
                    point = new GeoPoint(point.Latitude + 1e-10, point.Longitude);
                }

                if (point.Latitude > a.Latitude && point.Latitude < b.Latitude &&
                    point.Longitude < (b.Longitude - a.Longitude) *
                    (point.Latitude - a.Latitude) / (b.Latitude - a.Latitude) + a.Longitude)
                {
                    crossings++;
                }
            }

            return crossings % 2 == 1;
        }

        /// <summary>
        /// Calculates the approximate area of the polygon on the Earth's surface using spherical excess.
        /// </summary>
        /// <param name="unit">The unit of the resulting area (default: square kilometers).</param>
        /// <returns>The area of the polygon.</returns>
        public double Area(AreaUnit unit = AreaUnit.SquareKilometers)
        {
            double total = 0;

            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                var p1 = Vertices[i];
                var p2 = Vertices[i + 1];

                var lon1 = AngleConverter.DegreesToRadians(p1.Longitude);
                var lat1 = AngleConverter.DegreesToRadians(p1.Latitude);
                var lon2 = AngleConverter.DegreesToRadians(p2.Longitude);
                var lat2 = AngleConverter.DegreesToRadians(p2.Latitude);

                total += (lon2 - lon1) * (2 + Math.Sin(lat1) + Math.Sin(lat2));
            }
            double area = Math.Abs(total) * GeoConstants.EarthRadiusKm * GeoConstants.EarthRadiusKm / 2;

            return AreaConverter.FromSquareKilometers(area, unit);
        }
    }
}
