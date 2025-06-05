using GeoCore.Core;
using GeoCore.Units;

namespace GeoCore.Tests.Core
{
    [TestFixture]
    public class GeoPolygonTests
    {
        private GeoPolygon _square;

        [SetUp]
        public void Setup()
        {
            _square = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(0, 10),
                new GeoPoint(10, 10),
                new GeoPoint(10, 0),
                new GeoPoint(0, 0) // closed loop
            });
        }

        [Test]
        public void Contains_PointInside_ReturnsTrue()
        {
            var point = new GeoPoint(5, 5);
            Assert.That(_square.Contains(point), Is.True);
        }

        [Test]
        public void Contains_PointOutside_ReturnsFalse()
        {
            var point = new GeoPoint(15, 5);
            Assert.That(_square.Contains(point), Is.False);
        }

        [Test]
        public void Contains_PointOnEdge_ReturnsTrue()
        {
            var point = new GeoPoint(0, 5);
            Assert.That(_square.Contains(point), Is.True); // edge case
        }

        [Test]
        public void Contains_PointExactlyAtVertex_ReturnsTrue()
        {
            var point = new GeoPoint(0, 0);
            Assert.That(_square.Contains(point), Is.True); // also considered outside
        }

        [Test]
        public void Constructor_ThrowsIfLessThanThreeVertices()
        {
            var vertices = new[] {
                new GeoPoint(0, 0),
                new GeoPoint(0, 1)
            };

            Assert.Throws<ArgumentException>(() => new GeoPolygon(vertices));
        }

        [Test]
        public void BoundingBox_IsCorrect()
        {
            var bbox = _square.BoundingBox;

            Assert.That(bbox.MinLatitude, Is.EqualTo(0));
            Assert.That(bbox.MaxLatitude, Is.EqualTo(10));
            Assert.That(bbox.MinLongitude, Is.EqualTo(0));
            Assert.That(bbox.MaxLongitude, Is.EqualTo(10));
        }

        [Test]
        public void Area_DefaultUnit_ReturnsApprox1239552Km2()
        {
            var area = _square.Area(); // km²
            Assert.That(area, Is.InRange(1_230_000, 1_250_000));
        }

        [Test]
        public void Area_SquareMeters_ReturnsExpected()
        {
            var area = _square.Area(AreaUnit.SquareMeters);
            Assert.That(area, Is.InRange(1_230_000_000_000, 1_250_000_000_000));
        }

        [Test]
        public void Area_SquareMiles_ReturnsExpected()
        {
            var area = _square.Area(AreaUnit.SquareMiles);
            Assert.That(area, Is.InRange(474_000, 476_000));
        }

        [Test]
        public void Area_Hectares_ReturnsExpected()
        {
            var area = _square.Area(AreaUnit.Hectares);
            Assert.That(area, Is.InRange(123_000_000, 125_000_000));
        }

        [Test]
        public void Area_Acres_ReturnsExpected()
        {
            var area = _square.Area(AreaUnit.Acres);
            Assert.That(area, Is.InRange(303_000_000, 308_000_000));
        }
    }
}
