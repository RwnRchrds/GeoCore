using GeoCore.Core;
using GeoCore.Extensions;
using GeoCore.Units;

namespace GeoCore.Tests.Core
{
    [TestFixture]
    public class GeoCircleTests
    {
        private static readonly GeoPoint London = new(51.5074, -0.1278);

        [Test]
        public void Contains_IncludesPointsInsideAndExcludesPointsOutside()
        {
            var circle = new GeoCircle(London, 10);

            Assert.Multiple(() =>
            {
                Assert.That(circle.Contains(London), Is.True);
                Assert.That(circle.Contains(London.Move(9.9, 45)), Is.True);
                Assert.That(circle.Contains(London.Move(10.1, 45)), Is.False);
            });
        }

        [Test]
        public void Radius_ConvertsBetweenUnits()
        {
            var circle = new GeoCircle(London, 5, DistanceUnit.Miles);

            Assert.Multiple(() =>
            {
                Assert.That(circle.RadiusMeters, Is.EqualTo(5 * 1609.344).Within(1e-9));
                Assert.That(circle.Radius(DistanceUnit.Miles), Is.EqualTo(5).Within(1e-9));
                Assert.That(circle.Radius(DistanceUnit.Kilometers), Is.EqualTo(8.04672).Within(1e-9));
            });
        }

        [Test]
        public void Area_ForASmallCircle_IsCloseToTheFlatApproximation()
        {
            var circle = new GeoCircle(London, 10);

            Assert.That(circle.Area(), Is.EqualTo(Math.PI * 100).Within(0.001));
        }

        [Test]
        public void Area_ForAHemisphere_IsHalfTheEarthsSurface()
        {
            var quarterCircumference = 2 * Math.PI * GeoConstants.EarthRadiusKm / 4;
            var circle = new GeoCircle(GeoPoint.NorthPole, quarterCircumference);

            var hemisphere = 2 * Math.PI * GeoConstants.EarthRadiusKm * GeoConstants.EarthRadiusKm;

            Assert.That(circle.Area(), Is.EqualTo(hemisphere).Within(0.001));
        }

        [Test]
        public void Circumference_ForASmallCircle_IsCloseToTheFlatApproximation()
        {
            var circle = new GeoCircle(London, 10);

            Assert.That(circle.Circumference(), Is.EqualTo(2 * Math.PI * 10).Within(0.001));
        }

        [Test]
        public void BoundingBox_ContainsTheWholeCircle()
        {
            var circle = new GeoCircle(London, 25);
            var box = circle.BoundingBox;

            // Sample the circle's edge all the way round.
            for (var bearing = 0; bearing < 360; bearing += 15)
            {
                Assert.That(box.Contains(London.Move(25, bearing)), Is.True,
                    $"The bounding box should contain the circle's edge at {bearing}°.");
            }
        }

        [Test]
        public void ToPolygon_ProducesVerticesOnTheCircle()
        {
            var circle = new GeoCircle(London, 10);
            var polygon = circle.ToPolygon(36);

            Assert.Multiple(() =>
            {
                Assert.That(polygon.Vertices, Has.Count.EqualTo(36));
                Assert.That(polygon.Vertices.Select(v => London.DistanceTo(v)),
                    Is.All.EqualTo(10).Within(1e-6));
            });
        }

        [Test]
        public void ToPolygon_AreaApproachesTheCircleAreaAsSegmentsIncrease()
        {
            var circle = new GeoCircle(London, 10);

            var coarse = Math.Abs(circle.ToPolygon(8).Area() - circle.Area());
            var fine = Math.Abs(circle.ToPolygon(256).Area() - circle.Area());

            Assert.That(fine, Is.LessThan(coarse));
        }

        [Test]
        public void ToPolygon_WithTooFewSegments_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCircle(London, 10).ToPolygon(2));
        }

        [Test]
        public void Intersects_DetectsOverlappingCircles()
        {
            var circle = new GeoCircle(London, 10);

            Assert.Multiple(() =>
            {
                Assert.That(circle.Intersects(new GeoCircle(London.Move(15, 90), 10)), Is.True);
                Assert.That(circle.Intersects(new GeoCircle(London.Move(25, 90), 10)), Is.False);
            });
        }

        [Test]
        public void Contains_DetectsAWhollyEnclosedCircle()
        {
            var circle = new GeoCircle(London, 20);

            Assert.Multiple(() =>
            {
                Assert.That(circle.Contains(new GeoCircle(London.Move(5, 0), 10)), Is.True);
                Assert.That(circle.Contains(new GeoCircle(London.Move(15, 0), 10)), Is.False);
            });
        }

        [Test]
        public void Constructor_WithAnInvalidRadius_Throws()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCircle(London, -1));
                Assert.Throws<ArgumentOutOfRangeException>(() => new GeoCircle(London, double.NaN));
            });
        }

        [Test]
        public void Equality_ComparesCentreAndRadius()
        {
            var tenKilometres = new GeoCircle(London, 10);
            var sameAgain = new GeoCircle(London, 10);
            var tenThousandMetres = new GeoCircle(London, 10_000, DistanceUnit.Meters);
            var elevenKilometres = new GeoCircle(London, 11);

            Assert.Multiple(() =>
            {
                Assert.That(sameAgain, Is.EqualTo(tenKilometres));
                Assert.That(tenThousandMetres, Is.EqualTo(tenKilometres),
                    "The unit a radius was given in should not affect equality.");
                Assert.That(elevenKilometres, Is.Not.EqualTo(tenKilometres));
            });
        }
    }
}
