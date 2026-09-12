using GeoCore.Core;
using GeoCore.Units;

namespace GeoCore.Tests.Core
{
    [TestFixture]
    public class GeoBoundingBoxOperationsTests
    {
        [Test]
        public void Constructor_RejectsAnInvertedLatitudeRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GeoBoundingBox(52, 0, 50, 1));
        }

        [Test]
        public void Constructor_AllowsAnInvertedLongitudeRange_MeaningItWraps()
        {
            var box = new GeoBoundingBox(-10, 170, 10, -170);

            Assert.That(box.CrossesAntimeridian, Is.True);
        }

        [Test]
        public void LongitudeSpan_AccountsForWrapping()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new GeoBoundingBox(0, -10, 10, 10).LongitudeSpan, Is.EqualTo(20));
                Assert.That(new GeoBoundingBox(0, 170, 10, -170).LongitudeSpan, Is.EqualTo(20));
            });
        }

        [Test]
        public void Contains_HandlesAWrappedBox()
        {
            var box = new GeoBoundingBox(-10, 170, 10, -170);

            Assert.Multiple(() =>
            {
                Assert.That(box.Contains(new GeoPoint(0, 175)), Is.True);
                Assert.That(box.Contains(new GeoPoint(0, -175)), Is.True);
                Assert.That(box.Contains(new GeoPoint(0, 180)), Is.True);
                Assert.That(box.Contains(new GeoPoint(0, 0)), Is.False);
                Assert.That(box.Contains(new GeoPoint(0, 160)), Is.False);
                Assert.That(box.Contains(new GeoPoint(20, 175)), Is.False, "Outside the latitude band.");
            });
        }

        [Test]
        public void Center_OfAWrappedBox_SitsOnTheAntimeridian()
        {
            var center = new GeoBoundingBox(-10, 170, 10, -170).Center;

            Assert.Multiple(() =>
            {
                Assert.That(center.Latitude, Is.EqualTo(0));
                Assert.That(Math.Abs(center.Longitude), Is.EqualTo(180).Within(1e-9));
            });
        }

        [Test]
        public void Corners_AreTheFourEdgeCombinations()
        {
            var box = new GeoBoundingBox(50, -1, 52, 1);

            Assert.Multiple(() =>
            {
                Assert.That(box.SouthWest, Is.EqualTo(new GeoPoint(50, -1)));
                Assert.That(box.NorthEast, Is.EqualTo(new GeoPoint(52, 1)));
                Assert.That(box.NorthWest, Is.EqualTo(new GeoPoint(52, -1)));
                Assert.That(box.SouthEast, Is.EqualTo(new GeoPoint(50, 1)));
            });
        }

        [Test]
        public void FromPoints_EnclosesEveryPoint()
        {
            var points = new[]
            {
                new GeoPoint(51.5074, -0.1278), new GeoPoint(48.8566, 2.3522), new GeoPoint(52.3676, 4.9041)
            };

            var box = GeoBoundingBox.FromPoints(points);

            Assert.That(points, Is.All.Matches<GeoPoint>(p => box.Contains(p)));
        }

        [Test]
        public void FromPoints_AcrossTheAntimeridian_ProducesANarrowWrappedBox()
        {
            // Naively taking min and max would give a box spanning 358 degrees.
            var points = new[] { new GeoPoint(0, 179), new GeoPoint(0, -179) };

            var box = GeoBoundingBox.FromPoints(points);

            Assert.Multiple(() =>
            {
                Assert.That(box.CrossesAntimeridian, Is.True);
                Assert.That(box.LongitudeSpan, Is.EqualTo(2).Within(1e-9));
                Assert.That(points, Is.All.Matches<GeoPoint>(p => box.Contains(p)));
            });
        }

        [Test]
        public void FromPoints_WithASinglePoint_ProducesAnEmptyBoxAtThatPoint()
        {
            var box = GeoBoundingBox.FromPoints(new GeoPoint(51.5, -0.12));

            Assert.Multiple(() =>
            {
                Assert.That(box.IsEmpty, Is.True);
                Assert.That(box.Contains(new GeoPoint(51.5, -0.12)), Is.True);
            });
        }

        [Test]
        public void FromPoints_WithNoPoints_Throws()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(() => GeoBoundingBox.FromPoints(Array.Empty<GeoPoint>()));
                Assert.Throws<ArgumentNullException>(
                    () => GeoBoundingBox.FromPoints((IEnumerable<GeoPoint>)null!));
            });
        }

        [Test]
        public void Union_EnclosesBothBoxes()
        {
            var a = new GeoBoundingBox(0, 0, 10, 10);
            var b = new GeoBoundingBox(5, 5, 20, 20);

            var union = a.Union(b);

            Assert.Multiple(() =>
            {
                Assert.That(union.MinLatitude, Is.EqualTo(0));
                Assert.That(union.MaxLatitude, Is.EqualTo(20));
                Assert.That(union.MinLongitude, Is.EqualTo(0));
                Assert.That(union.MaxLongitude, Is.EqualTo(20));
                Assert.That(union.Contains(a), Is.True);
                Assert.That(union.Contains(b), Is.True);
            });
        }

        [Test]
        public void Union_AcrossTheAntimeridian_TakesTheShortWayRound()
        {
            var a = new GeoBoundingBox(0, 175, 10, 179);
            var b = new GeoBoundingBox(0, -179, 10, -175);

            var union = a.Union(b);

            Assert.Multiple(() =>
            {
                Assert.That(union.CrossesAntimeridian, Is.True);
                Assert.That(union.LongitudeSpan, Is.EqualTo(10).Within(1e-9));
                Assert.That(union.Contains(a), Is.True);
                Assert.That(union.Contains(b), Is.True);
            });
        }

        [Test]
        public void Union_IsCommutative()
        {
            var a = new GeoBoundingBox(0, 175, 10, 179);
            var b = new GeoBoundingBox(0, -179, 10, -175);

            Assert.That(a.Union(b), Is.EqualTo(b.Union(a)));
        }

        [Test]
        public void Intersects_DetectsOverlapAndSeparation()
        {
            var box = new GeoBoundingBox(0, 0, 10, 10);

            Assert.Multiple(() =>
            {
                Assert.That(box.Intersects(new GeoBoundingBox(5, 5, 15, 15)), Is.True);
                Assert.That(box.Intersects(new GeoBoundingBox(20, 20, 30, 30)), Is.False);
                Assert.That(box.Intersects(new GeoBoundingBox(10, 10, 20, 20)), Is.True, "Touching counts.");
                Assert.That(box.Intersects(new GeoBoundingBox(0, 20, 10, 30)), Is.False,
                    "Same latitudes but disjoint longitudes.");
            });
        }

        [Test]
        public void Intersects_HandlesAWrappedBox()
        {
            var wrapped = new GeoBoundingBox(-10, 170, 10, -170);

            Assert.Multiple(() =>
            {
                Assert.That(wrapped.Intersects(new GeoBoundingBox(0, 175, 5, 178)), Is.True);
                Assert.That(wrapped.Intersects(new GeoBoundingBox(0, -178, 5, -175)), Is.True);
                Assert.That(wrapped.Intersects(new GeoBoundingBox(0, 0, 5, 10)), Is.False);
            });
        }

        [Test]
        public void Contains_ABoxRequiresCompleteEnclosure()
        {
            var box = new GeoBoundingBox(0, 0, 10, 10);

            Assert.Multiple(() =>
            {
                Assert.That(box.Contains(new GeoBoundingBox(2, 2, 8, 8)), Is.True);
                Assert.That(box.Contains(new GeoBoundingBox(2, 2, 12, 8)), Is.False);
                Assert.That(box.Contains(box), Is.True);
            });
        }

        [Test]
        public void Expand_GrowsTheBoxInEveryDirection()
        {
            var box = new GeoBoundingBox(50, -1, 52, 1);
            var expanded = box.Expand(10);

            Assert.Multiple(() =>
            {
                Assert.That(expanded.Contains(box), Is.True);
                Assert.That(expanded.MinLatitude, Is.LessThan(box.MinLatitude));
                Assert.That(expanded.MaxLatitude, Is.GreaterThan(box.MaxLatitude));
                Assert.That(expanded.LongitudeSpan, Is.GreaterThan(box.LongitudeSpan));
            });
        }

        [Test]
        public void Expand_ReachingAPole_WidensToEveryLongitude()
        {
            var box = new GeoBoundingBox(89.9, -1, 89.95, 1);
            var expanded = box.Expand(50);

            Assert.Multiple(() =>
            {
                Assert.That(expanded.MaxLatitude, Is.EqualTo(90));
                Assert.That(expanded.LongitudeSpan, Is.EqualTo(360));
            });
        }

        [Test]
        public void Expand_WithANegativeDistance_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GeoBoundingBox.World.Expand(-1));
        }

        [Test]
        public void Area_OfTheWholeWorld_MatchesTheSpheresSurfaceArea()
        {
            var expected = 4 * Math.PI * GeoConstants.EarthRadiusKm * GeoConstants.EarthRadiusKm;

            Assert.That(GeoBoundingBox.World.Area(), Is.EqualTo(expected).Within(1e-3));
        }

        [Test]
        public void Area_OfAnEquatorialBand_IsPositiveAndUnitAware()
        {
            var box = new GeoBoundingBox(0, 0, 1, 1);

            Assert.Multiple(() =>
            {
                Assert.That(box.Area(AreaUnit.SquareKilometers), Is.GreaterThan(0));
                Assert.That(box.Area(AreaUnit.SquareMeters),
                    Is.EqualTo(box.Area(AreaUnit.SquareKilometers) * 1_000_000).Within(1e-3));
            });
        }

        [Test]
        public void Deconstruct_YieldsTheFourEdges()
        {
            var (minLatitude, minLongitude, maxLatitude, maxLongitude) =
                new GeoBoundingBox(50, -1, 52, 1);

            Assert.Multiple(() =>
            {
                Assert.That(minLatitude, Is.EqualTo(50));
                Assert.That(minLongitude, Is.EqualTo(-1));
                Assert.That(maxLatitude, Is.EqualTo(52));
                Assert.That(maxLongitude, Is.EqualTo(1));
            });
        }
    }
}
