using GeoCore.Core;
using GeoCore.Units;
using GeoCore.Extensions;

namespace GeoCore.Tests.Extensions
{
    [TestFixture]
    public class GeoPointExtensionsTests
    {
        private readonly GeoPoint london = new(51.5074, -0.1278);
        private readonly GeoPoint paris = new(48.8566, 2.3522);
        private readonly GeoPoint newYork = new(40.7128, -74.0060);

        [Test]
        public void DistanceTo_LondonToParis_IsRoughly343Km()
        {
            var distance = london.DistanceTo(paris, DistanceUnit.Kilometers);
            Assert.That(distance, Is.InRange(340, 350));
        }

        [Test]
        public void DistanceTo_LondonToNewYork_IsRoughly5567Km()
        {
            var distance = london.DistanceTo(newYork, DistanceUnit.Kilometers);
            Assert.That(distance, Is.InRange(5500, 5700));
        }

        [Test]
        public void BearingTo_LondonToParis_IsRoughly149Degrees()
        {
            var bearing = london.BearingTo(paris);
            Assert.That(bearing, Is.InRange(147, 151));
        }

        [Test]
        public void BearingTo_LondonToNewYork_IsRoughly288Degrees()
        {
            var bearing = london.BearingTo(newYork);
            Assert.That(bearing, Is.InRange(285, 295));
        }

        [Test]
        public void Move_FromLondonEast250Km_LongitudeShouldIncrease()
        {
            var result = london.Move(250, 90, DistanceUnit.Kilometers); // 90° = east
            Assert.That(result.Longitude, Is.GreaterThan(london.Longitude));
            Assert.That(result.Latitude, Is.InRange(50.5, 52)); // Slight southward dip
        }

        [Test]
        public void Move_FromParisNorth100Km_EndsFurtherNorth()
        {
            var result = paris.Move(100, 0, DistanceUnit.Kilometers); // 0° = north
            Assert.That(result.Latitude, Is.GreaterThan(paris.Latitude));
        }

        [Test]
        public void GetBoundingBox_10KmRadius_London_BoxIsReasonable()
        {
            var box = london.GetBoundingBox(10, DistanceUnit.Kilometers);

            // Latitude change for 10km should be about ±0.09
            Assert.That(box.MinLatitude, Is.InRange(51.40, 51.50));
            Assert.That(box.MaxLatitude, Is.InRange(51.51, 51.60));

            // Longitude change should be roughly ±0.15 at this latitude
            Assert.That(box.MinLongitude, Is.LessThan(london.Longitude));
            Assert.That(box.MaxLongitude, Is.GreaterThan(london.Longitude));
        }

        [Test]
        public void GetBoundingBox_ContainsCenterPoint()
        {
            var box = london.GetBoundingBox(5);

            Assert.That(box.Contains(london), Is.True);
        }
        
        [Test]
        public void ToDmsString_NorthernEasternHemisphere_ReturnsCorrectFormat()
        {
            var point = new GeoPoint(51.5074, 0.1278); // London
            var result = point.ToDmsString();
            Assert.That(result, Is.EqualTo("51°30'26.64\"N, 0°07'40.08\"E"));
        }

        [Test]
        public void ToDmsString_SouthernWesternHemisphere_ReturnsCorrectFormat()
        {
            var point = new GeoPoint(-33.9249, -18.4241); // Cape Town
            var result = point.ToDmsString();
            Assert.That(result, Is.EqualTo("33°55'29.64\"S, 18°25'26.76\"W"));
        }

        [Test]
        public void ToDmsString_ZeroCoordinates_ReturnsCorrectFormat()
        {
            var point = new GeoPoint(0, 0);
            var result = point.ToDmsString();
            Assert.That(result, Is.EqualTo("0°00'00.00\"N, 0°00'00.00\"E"));
        }

        [Test]
        public void ToDmsString_PrecisionIsCorrect()
        {
            var point = new GeoPoint(10.123456, -20.654321);
            var result = point.ToDmsString();
            Assert.That(result, Is.EqualTo("10°07'24.44\"N, 20°39'15.56\"W"));
        }
    }
}
