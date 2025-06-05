using GeoCore.Core;

namespace GeoCore.Tests.Core
{
    [TestFixture]
    public class GeoBoundingBoxTests
    {
        [Test]
        public void Contains_PointInsideBox_ReturnsTrue()
        {
            var box = new GeoBoundingBox(
                MinLatitude: 50,
                MinLongitude: -1,
                MaxLatitude: 52,
                MaxLongitude: 1
            );

            var point = new GeoPoint(51, 0); // Clearly inside
            Assert.That(box.Contains(point), Is.True);
        }

        [Test]
        public void Contains_PointOutsideBox_ReturnsFalse()
        {
            var box = new GeoBoundingBox(
                MinLatitude: 50,
                MinLongitude: -1,
                MaxLatitude: 52,
                MaxLongitude: 1
            );

            var point = new GeoPoint(53, 0); // Outside in latitude
            Assert.That(box.Contains(point), Is.False);
        }

        [Test]
        public void Contains_PointOnEdge_ReturnsTrue()
        {
            var box = new GeoBoundingBox(
                MinLatitude: 50,
                MinLongitude: -1,
                MaxLatitude: 52,
                MaxLongitude: 1
            );

            var point = new GeoPoint(50, 1); // On min lat, max lon
            Assert.That(box.Contains(point), Is.True);
        }
    }
}
