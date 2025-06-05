using GeoCore.Core;

namespace GeoCore.Tests.Core
{
    [TestFixture]
    public class GeoPointTests
    {
        [Test]
        public void Constructor_SetsLatitudeAndLongitude()
        {
            var point = new GeoPoint(51.5, -0.12);
            Assert.That(point.Latitude, Is.EqualTo(51.5));
            Assert.That(point.Longitude, Is.EqualTo(-0.12));
        }

        [Test]
        public void ToString_ReturnsFormattedString()
        {
            var point = new GeoPoint(51.5074, -0.1278);
            var expected = "GeoPoint(Latitude: 51.507400, Longitude: -0.127800)";
            Assert.That(point.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void EqualsWithTolerance_WhenWithinTolerance_ReturnsTrue()
        {
            var a = new GeoPoint(51.5, -0.12);
            var b = new GeoPoint(51.5000009, -0.1200008);

            Assert.That(a.EqualsWithTolerance(b), Is.True);
        }

        [Test]
        public void EqualsWithTolerance_WhenOutsideTolerance_ReturnsFalse()
        {
            var a = new GeoPoint(51.5, -0.12);
            var b = new GeoPoint(51.6, -0.12);

            Assert.That(a.EqualsWithTolerance(b), Is.False);
        }

        [Test]
        public void EqualsWithTolerance_WithCustomTolerance_ReturnsExpected()
        {
            var a = new GeoPoint(10.0000, 20.0000);
            var b = new GeoPoint(10.0051, 20.0049);

            Assert.That(a.EqualsWithTolerance(b, tolerance: 0.01), Is.True);
            Assert.That(a.EqualsWithTolerance(b, tolerance: 0.001), Is.False);
        }
    }
}
