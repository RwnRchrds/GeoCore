using GeoCore.Core;
using GeoCore.Formats;

namespace GeoCore.Tests.Formats
{
    [TestFixture]
    public class WktTests
    {
        [Test]
        public void Write_APoint_PutsLongitudeFirst()
        {
            Assert.That(Wkt.Write(new GeoPoint(51.5074, -0.1278)),
                Is.EqualTo("POINT (-0.1278 51.5074)"));
        }

        [Test]
        public void ReadPoint_ParsesLongitudeFirst()
        {
            var point = Wkt.ReadPoint("POINT (-0.1278 51.5074)");

            Assert.Multiple(() =>
            {
                Assert.That(point.Latitude, Is.EqualTo(51.5074));
                Assert.That(point.Longitude, Is.EqualTo(-0.1278));
            });
        }

        [TestCase("POINT(-0.1278 51.5074)")]
        [TestCase("point (-0.1278 51.5074)")]
        [TestCase("  POINT   (  -0.1278   51.5074  )  ")]
        [TestCase("POINT Z (-0.1278 51.5074 42)")]
        public void ReadPoint_AcceptsTheUsualFormattingVariations(string wkt)
        {
            Assert.That(Wkt.ReadPoint(wkt), Is.EqualTo(new GeoPoint(51.5074, -0.1278)));
        }

        [Test]
        public void Write_ARoute_ProducesALineString()
        {
            var route = new GeoRoute(new[] { new GeoPoint(0, 0), new GeoPoint(1, 2) });

            Assert.That(Wkt.Write(route), Is.EqualTo("LINESTRING (0 0, 2 1)"));
        }

        [Test]
        public void ReadRoute_RoundTripsALineString()
        {
            var route = new GeoRoute(new[]
            {
                new GeoPoint(51.5074, -0.1278), new GeoPoint(48.8566, 2.3522), new GeoPoint(41.9028, 12.4964)
            });

            Assert.That(Wkt.ReadRoute(Wkt.Write(route)).Points, Is.EqualTo(route.Points));
        }

        [Test]
        public void Write_APolygon_ClosesTheRing()
        {
            var polygon = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0), new GeoPoint(0, 1), new GeoPoint(1, 1)
            });

            // The first vertex is repeated at the end, as WKT requires.
            Assert.That(Wkt.Write(polygon), Is.EqualTo("POLYGON ((0 0, 1 0, 1 1, 0 0))"));
        }

        [Test]
        public void ReadPolygon_RoundTripsAPolygonWithHoles()
        {
            var polygon = new GeoPolygon(
                new[] { new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10), new GeoPoint(10, 0) },
                new[]
                {
                    new[] { new GeoPoint(4, 4), new GeoPoint(4, 6), new GeoPoint(6, 6), new GeoPoint(6, 4) }
                });

            var parsed = Wkt.ReadPolygon(Wkt.Write(polygon));

            Assert.Multiple(() =>
            {
                Assert.That(parsed.Vertices, Is.EqualTo(polygon.Vertices));
                Assert.That(parsed.Holes, Has.Count.EqualTo(1));
                Assert.That(parsed.Holes[0], Is.EqualTo(polygon.Holes[0]));
            });
        }

        [Test]
        public void ReadPolygon_AcceptsAnExplicitlyClosedRingWithoutDuplicatingTheVertex()
        {
            var parsed = Wkt.ReadPolygon("POLYGON ((0 0, 1 0, 1 1, 0 0))");

            Assert.That(parsed.Vertices, Has.Count.EqualTo(3));
        }

        [Test]
        public void Write_ABoundingBox_ProducesAClosedRectangle()
        {
            var box = new GeoBoundingBox(0, 0, 1, 1);

            Assert.That(Wkt.Write(box), Is.EqualTo("POLYGON ((0 0, 1 0, 1 1, 0 1, 0 0))"));
        }

        [Test]
        public void ReadPoint_WithTheWrongGeometryType_Throws()
        {
            var exception = Assert.Throws<FormatException>(
                () => Wkt.ReadPoint("LINESTRING (0 0, 1 1)"));

            Assert.That(exception!.Message, Does.Contain("POINT"));
        }

        [Test]
        public void ReadPoint_WithSwappedCoordinates_ExplainsTheOrdering()
        {
            // 51.5 as a longitude is fine, but 200 as a latitude is not.
            var exception = Assert.Throws<FormatException>(() => Wkt.ReadPoint("POINT (51.5 200)"));

            Assert.That(exception!.Message, Does.Contain("longitude before latitude"));
        }

        [TestCase("POINT (0 0) extra")]
        [TestCase("POINT (0)")]
        [TestCase("POINT 0 0")]
        [TestCase("NONSENSE (0 0)")]
        public void ReadPoint_WithMalformedText_Throws(string wkt)
        {
            Assert.Throws<FormatException>(() => Wkt.ReadPoint(wkt));
        }

        [Test]
        public void TryReadPoint_ReportsFailureWithoutThrowing()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Wkt.TryReadPoint("nonsense", out _), Is.False);
                Assert.That(Wkt.TryReadPoint(null, out _), Is.False);
                Assert.That(Wkt.TryReadPoint("POINT (1 2)", out var point), Is.True);
                Assert.That(point, Is.EqualTo(new GeoPoint(2, 1)));
            });
        }

        [Test]
        public void ReadRoute_WithASingleCoordinate_Throws()
        {
            Assert.Throws<FormatException>(() => Wkt.ReadRoute("LINESTRING (0 0)"));
        }
    }
}
