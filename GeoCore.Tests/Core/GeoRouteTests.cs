using GeoCore.Core;
using GeoCore.Units;

namespace GeoCore.Tests.Core
{
    [TestFixture]
    public class GeoRouteTests
    {
        [Test]
        public void TotalDistance_TwoPointsLondonToParis_ReturnsReasonableDistance()
        {
            var london = new GeoPoint(51.5074, -0.1278);
            var paris = new GeoPoint(48.8566, 2.3522);

            var route = new GeoRoute(new[] { london, paris });

            var distance = route.TotalDistance(DistanceUnit.Kilometers);

            Assert.That(distance, Is.InRange(340, 360)); // ~344 km expected
        }

        [Test]
        public void Constructor_ThrowsIfLessThanTwoPoints()
        {
            var single = new[] { new GeoPoint(51.5, 0) };
            Assert.Throws<ArgumentException>(() => new GeoRoute(single));
        }

        [Test]
        public void StartAndEnd_ReturnCorrectPoints()
        {
            var a = new GeoPoint(0, 0);
            var b = new GeoPoint(1, 1);
            var c = new GeoPoint(2, 2);

            var route = new GeoRoute(new[] { a, b, c });

            Assert.That(route.Start, Is.EqualTo(a));
            Assert.That(route.End, Is.EqualTo(c));
        }

        [Test]
        public void BearingsBetweenPoints_ReturnsExpectedCountAndValues()
        {
            var a = new GeoPoint(0, 0);
            var b = new GeoPoint(0, 1);   // Due East
            var c = new GeoPoint(1, 1);   // Due North

            var route = new GeoRoute(new[] { a, b, c });
            var bearings = route.BearingsBetweenPoints();

            Assert.That(bearings.Count, Is.EqualTo(2));
            Assert.That(IsAngleBetween(bearings[0], 89, 91));   // ~90°
            Assert.That(IsAngleBetween(bearings[1], 359, 1)); // ~0° (north, slight curve)
        }

        [Test]
        public void MoveAlongRoute_MidwayBetweenTwoPoints_ReturnsCorrectlyMovedPoint()
        {
            var a = new GeoPoint(0, 0);
            var b = new GeoPoint(0, 2);
            var route = new GeoRoute(new[] { a, b });

            var mid = route.MoveAlongRoute(111, DistanceUnit.Kilometers); // ~111km = 1° longitude at equator

            Assert.That(mid.Latitude, Is.EqualTo(0).Within(0.01));
            Assert.That(mid.Longitude, Is.EqualTo(1).Within(0.01));
        }

        [Test]
        public void MoveAlongRouteByFraction_HalfwayBetweenTwoPoints_ReturnsMidpoint()
        {
            var a = new GeoPoint(0, 0);
            var b = new GeoPoint(0, 2);
            var route = new GeoRoute(new[] { a, b });

            var mid = route.MoveAlongRouteByFraction(0.5);

            Assert.That(mid.Latitude, Is.EqualTo(0).Within(0.01));
            Assert.That(mid.Longitude, Is.EqualTo(1).Within(0.01));
        }

        [Test]
        public void MoveAlongRouteByFraction_ThrowsIfOutOfBounds()
        {
            var route = new GeoRoute(new[] {
                new GeoPoint(0, 0),
                new GeoPoint(1, 1)
            });

            Assert.Throws<ArgumentOutOfRangeException>(() => route.MoveAlongRouteByFraction(-0.1));
            Assert.Throws<ArgumentOutOfRangeException>(() => route.MoveAlongRouteByFraction(1.1));
        }

        [Test]
        public void InterpolatePoints_ReturnsCorrectNumberAndRange()
        {
            var route = new GeoRoute(new[]
            {
                new GeoPoint(0, 0),
                new GeoPoint(0, 2)
            });

            var interpolated = route.InterpolatePoints(5);

            Assert.That(interpolated.Count, Is.EqualTo(5));
            Assert.That(interpolated.First().Longitude, Is.EqualTo(0).Within(0.01));
            Assert.That(interpolated.Last().Longitude, Is.EqualTo(2).Within(0.01));
            Assert.That(interpolated[2].Longitude, Is.EqualTo(1).Within(0.1)); // midpoint
        }


        private bool IsAngleBetween(double angle, double start, double end)
        {
            angle %= 360;
            start %= 360;
            end %= 360;

            if (start < end)
                return angle >= start && angle <= end;

            // Wraparound case
            return angle >= start || angle <= end;
        }
    }
}
