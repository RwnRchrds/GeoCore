using GeoCore.Core;
using GeoCore.Extensions;
using GeoCore.Units;

namespace GeoCore.Tests.Core
{
    [TestFixture]
    public class GeoRouteOperationsTests
    {
        private static GeoRoute EquatorRoute() => new(new[]
        {
            new GeoPoint(0, 0), new GeoPoint(0, 1), new GeoPoint(0, 2), new GeoPoint(0, 3)
        });

        [Test]
        public void Constructor_WithNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GeoRoute(null!));
        }

        [Test]
        public void SegmentCount_IsOneFewerThanThePointCount()
        {
            Assert.That(EquatorRoute().SegmentCount, Is.EqualTo(3));
        }

        [Test]
        public void Segments_YieldsConsecutivePairs()
        {
            var segments = EquatorRoute().Segments().ToList();

            Assert.Multiple(() =>
            {
                Assert.That(segments, Has.Count.EqualTo(3));
                Assert.That(segments[0].Start, Is.EqualTo(new GeoPoint(0, 0)));
                Assert.That(segments[0].End, Is.EqualTo(new GeoPoint(0, 1)));
                Assert.That(segments[2].End, Is.EqualTo(new GeoPoint(0, 3)));
            });
        }

        [Test]
        public void CumulativeDistances_StartAtZeroAndEndAtTheTotal()
        {
            var route = EquatorRoute();
            var cumulative = route.CumulativeDistances();

            Assert.Multiple(() =>
            {
                Assert.That(cumulative, Has.Count.EqualTo(4));
                Assert.That(cumulative[0], Is.Zero);
                Assert.That(cumulative[3], Is.EqualTo(route.TotalDistance()).Within(1e-9));
                Assert.That(cumulative, Is.Ordered);
            });
        }

        [Test]
        public void TotalDistance_ConvertsBetweenUnits()
        {
            var route = EquatorRoute();

            Assert.That(route.TotalDistance(DistanceUnit.Meters),
                Is.EqualTo(route.TotalDistance(DistanceUnit.Kilometers) * 1000).Within(1e-6));
        }

        [Test]
        public void MoveAlongRoute_BeyondTheEnd_ClampsToTheFinalPoint()
        {
            var route = EquatorRoute();

            Assert.That(route.MoveAlongRoute(100_000), Is.EqualTo(route.End));
        }

        [Test]
        public void MoveAlongRoute_LandsTheRequestedDistanceAlong()
        {
            var route = EquatorRoute();
            var target = route.TotalDistance() * 0.4;

            var point = route.MoveAlongRoute(target);

            Assert.That(route.ClosestPointTo(point).DistanceAlongRoute, Is.EqualTo(target).Within(1e-6));
        }

        [Test]
        public void MoveAlongRoute_WithANonFiniteDistance_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EquatorRoute().MoveAlongRoute(double.NaN));
        }

        [Test]
        public void MoveAlongRouteByFraction_MatchesTheEquivalentDistance()
        {
            var route = EquatorRoute();

            Assert.That(route.MoveAlongRouteByFraction(0.25),
                Is.EqualTo(route.MoveAlongRoute(route.TotalDistance() * 0.25)));
        }

        [Test]
        public void InterpolatePoints_AreEvenlySpacedAlongTheRoute()
        {
            var route = EquatorRoute();
            var points = route.InterpolatePoints(5);

            var gaps = points.Zip(points.Skip(1), (a, b) => a.DistanceTo(b)).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(points, Has.Count.EqualTo(5));
                Assert.That(points[0], Is.EqualTo(route.Start));
                Assert.That(points[4], Is.EqualTo(route.End));
                Assert.That(gaps, Is.All.EqualTo(gaps[0]).Within(1e-6));
            });
        }

        [Test]
        public void ClosestPointTo_FindsTheRightSegmentAndDistances()
        {
            var route = EquatorRoute();

            // A point due north of the middle of the second segment.
            var position = route.ClosestPointTo(new GeoPoint(0.5, 1.5));

            Assert.Multiple(() =>
            {
                Assert.That(position.SegmentIndex, Is.EqualTo(1));
                Assert.That(position.Point.Latitude, Is.EqualTo(0).Within(0.01));
                Assert.That(position.Point.Longitude, Is.EqualTo(1.5).Within(0.01));
                Assert.That(position.DistanceFromRoute, Is.EqualTo(55.6).Within(0.5));
            });
        }

        [Test]
        public void ClosestPointTo_ForAPointOnTheRoute_ReportsNoOffset()
        {
            var route = EquatorRoute();

            Assert.That(route.ClosestPointTo(new GeoPoint(0, 1.5)).DistanceFromRoute,
                Is.EqualTo(0).Within(1e-6));
        }

        [Test]
        public void ClosestPointTo_BeyondTheEnd_ClampsToTheFinalPoint()
        {
            var route = EquatorRoute();
            var position = route.ClosestPointTo(new GeoPoint(0, 10));

            Assert.That(position.Point, Is.EqualTo(route.End));
        }

        [Test]
        public void DistanceTo_MatchesTheClosestPointOffset()
        {
            var route = EquatorRoute();
            var point = new GeoPoint(0.5, 1.5);

            Assert.That(route.DistanceTo(point), Is.EqualTo(route.ClosestPointTo(point).DistanceFromRoute));
        }

        [Test]
        public void Reversed_SwapsTheEndsAndKeepsTheLength()
        {
            var route = EquatorRoute();
            var reversed = route.Reversed();

            Assert.Multiple(() =>
            {
                Assert.That(reversed.Start, Is.EqualTo(route.End));
                Assert.That(reversed.End, Is.EqualTo(route.Start));
                Assert.That(reversed.TotalDistance(), Is.EqualTo(route.TotalDistance()).Within(1e-9));
            });
        }

        [Test]
        public void Simplify_DropsPointsThatLieAlongTheLine()
        {
            // Five points strung along one straight run of the equator.
            var route = new GeoRoute(new[]
            {
                new GeoPoint(0, 0), new GeoPoint(0, 1), new GeoPoint(0, 2),
                new GeoPoint(0, 3), new GeoPoint(0, 4)
            });

            var simplified = route.Simplify(100, DistanceUnit.Meters);

            Assert.Multiple(() =>
            {
                Assert.That(simplified.Points, Has.Count.EqualTo(2));
                Assert.That(simplified.Start, Is.EqualTo(route.Start));
                Assert.That(simplified.End, Is.EqualTo(route.End));
            });
        }

        [Test]
        public void Simplify_KeepsPointsThatStrayBeyondTheTolerance()
        {
            var route = new GeoRoute(new[]
            {
                new GeoPoint(0, 0), new GeoPoint(1, 1), new GeoPoint(0, 2)
            });

            // The middle point is ~150 km off the straight line, well beyond 1 km.
            Assert.That(route.Simplify(1, DistanceUnit.Kilometers).Points, Has.Count.EqualTo(3));
        }

        [Test]
        public void Simplify_WithZeroTolerance_KeepsEveryPoint()
        {
            var route = EquatorRoute();

            Assert.That(route.Simplify(0).Points, Has.Count.EqualTo(route.Points.Count));
        }

        [Test]
        public void Simplify_WithANegativeTolerance_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EquatorRoute().Simplify(-1));
        }

        [Test]
        public void BoundingBox_EnclosesEveryPoint()
        {
            var route = new GeoRoute(new[]
            {
                new GeoPoint(51.5074, -0.1278), new GeoPoint(48.8566, 2.3522), new GeoPoint(52.3676, 4.9041)
            });

            Assert.That(route.Points, Is.All.Matches<GeoPoint>(p => route.BoundingBox.Contains(p)));
        }

        [Test]
        public void RepeatedQueries_AgreeWithEachOther()
        {
            // Distances are measured once and cached; make sure the cache is consistent.
            var route = EquatorRoute();

            var firstTotal = route.TotalDistance();
            var firstPoint = route.MoveAlongRoute(100);
            var firstCumulative = route.CumulativeDistances();

            var secondTotal = route.TotalDistance();
            var secondPoint = route.MoveAlongRoute(100);
            var secondCumulative = route.CumulativeDistances();

            Assert.Multiple(() =>
            {
                Assert.That(secondTotal, Is.EqualTo(firstTotal));
                Assert.That(secondPoint, Is.EqualTo(firstPoint));
                Assert.That(secondCumulative, Is.EqualTo(firstCumulative));
            });
        }

        [Test]
        public void ARouteOfIdenticalPoints_DoesNotDivideByZero()
        {
            var route = new GeoRoute(new[] { new GeoPoint(5, 5), new GeoPoint(5, 5) });

            Assert.Multiple(() =>
            {
                Assert.That(route.TotalDistance(), Is.Zero);
                Assert.That(route.MoveAlongRoute(10), Is.EqualTo(route.End));
                Assert.That(route.MoveAlongRouteByFraction(0.5), Is.EqualTo(route.Start));
                Assert.That(route.InterpolatePoints(3), Is.All.EqualTo(new GeoPoint(5, 5)));
            });
        }
    }
}
