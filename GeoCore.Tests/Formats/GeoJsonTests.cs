using System.Text.Json;
using GeoCore.Core;
using GeoCore.Formats;

namespace GeoCore.Tests.Formats
{
    [TestFixture]
    public class GeoJsonTests
    {
        [Test]
        public void Write_APoint_UsesLongitudeLatitudeOrder()
        {
            Assert.That(GeoJson.Write(new GeoPoint(51.5074, -0.1278)),
                Is.EqualTo("{\"type\":\"Point\",\"coordinates\":[-0.1278,51.5074]}"));
        }

        [Test]
        public void ReadPoint_ParsesLongitudeLatitudeOrder()
        {
            var point = GeoJson.ReadPoint("{\"type\":\"Point\",\"coordinates\":[-0.1278,51.5074]}");

            Assert.Multiple(() =>
            {
                Assert.That(point.Latitude, Is.EqualTo(51.5074));
                Assert.That(point.Longitude, Is.EqualTo(-0.1278));
            });
        }

        [Test]
        public void ReadPoint_UnwrapsAFeature()
        {
            const string json = """
                {"type":"Feature","properties":{"name":"London"},
                 "geometry":{"type":"Point","coordinates":[-0.1278,51.5074]}}
                """;

            Assert.That(GeoJson.ReadPoint(json), Is.EqualTo(new GeoPoint(51.5074, -0.1278)));
        }

        [Test]
        public void ReadRoute_RoundTripsALineString()
        {
            var route = new GeoRoute(new[]
            {
                new GeoPoint(51.5074, -0.1278), new GeoPoint(48.8566, 2.3522), new GeoPoint(41.9028, 12.4964)
            });

            Assert.That(GeoJson.ReadRoute(GeoJson.Write(route)).Points, Is.EqualTo(route.Points));
        }

        [Test]
        public void Write_APolygon_ClosesTheRing()
        {
            var polygon = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0), new GeoPoint(0, 1), new GeoPoint(1, 1)
            });

            using var document = JsonDocument.Parse(GeoJson.Write(polygon));
            var ring = document.RootElement.GetProperty("coordinates")[0];

            Assert.Multiple(() =>
            {
                Assert.That(ring.GetArrayLength(), Is.EqualTo(4), "The ring should be closed.");
                Assert.That(ring[0][0].GetDouble(), Is.EqualTo(ring[3][0].GetDouble()));
                Assert.That(ring[0][1].GetDouble(), Is.EqualTo(ring[3][1].GetDouble()));
            });
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

            var parsed = GeoJson.ReadPolygon(GeoJson.Write(polygon));

            Assert.Multiple(() =>
            {
                Assert.That(parsed.Vertices, Is.EqualTo(polygon.Vertices));
                Assert.That(parsed.Holes, Has.Count.EqualTo(1));
                Assert.That(parsed.Holes[0], Is.EqualTo(polygon.Holes[0]));
            });
        }

        [Test]
        public void Write_Indented_ProducesReadableOutput()
        {
            Assert.That(GeoJson.Write(new GeoPoint(1, 2), indented: true), Does.Contain("\n"));
        }

        [Test]
        public void ReadPoint_WithTheWrongGeometryType_Throws()
        {
            var exception = Assert.Throws<FormatException>(
                () => GeoJson.ReadPoint("{\"type\":\"LineString\",\"coordinates\":[[0,0],[1,1]]}"));

            Assert.That(exception!.Message, Does.Contain("Point"));
        }

        [Test]
        public void ReadPoint_WithSwappedCoordinates_ExplainsTheOrdering()
        {
            var exception = Assert.Throws<FormatException>(
                () => GeoJson.ReadPoint("{\"type\":\"Point\",\"coordinates\":[51.5,200]}"));

            Assert.That(exception!.Message, Does.Contain("longitude before latitude"));
        }

        [TestCase("not json at all")]
        [TestCase("{\"type\":\"Point\"}")]
        [TestCase("{\"coordinates\":[0,0]}")]
        [TestCase("{\"type\":\"Point\",\"coordinates\":[0]}")]
        [TestCase("[1,2]")]
        public void ReadPoint_WithMalformedJson_Throws(string json)
        {
            Assert.Throws<FormatException>(() => GeoJson.ReadPoint(json));
        }

        [Test]
        public void TryReadPoint_ReportsFailureWithoutThrowing()
        {
            Assert.Multiple(() =>
            {
                Assert.That(GeoJson.TryReadPoint("nope", out _), Is.False);
                Assert.That(GeoJson.TryReadPoint(null, out _), Is.False);
                Assert.That(GeoJson.TryReadPoint("{\"type\":\"Point\",\"coordinates\":[2,1]}", out var p), Is.True);
                Assert.That(p, Is.EqualTo(new GeoPoint(1, 2)));
            });
        }

        [Test]
        public void Write_ABoundingBox_ProducesAClosedPolygon()
        {
            using var document = JsonDocument.Parse(GeoJson.Write(new GeoBoundingBox(0, 0, 1, 1)));

            Assert.Multiple(() =>
            {
                Assert.That(document.RootElement.GetProperty("type").GetString(), Is.EqualTo("Polygon"));
                Assert.That(document.RootElement.GetProperty("coordinates")[0].GetArrayLength(), Is.EqualTo(5));
            });
        }
    }
}
