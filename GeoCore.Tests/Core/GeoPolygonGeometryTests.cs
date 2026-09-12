using GeoCore.Core;
using GeoCore.Extensions;
using GeoCore.Units;

namespace GeoCore.Tests.Core
{
    [TestFixture]
    public class GeoPolygonGeometryTests
    {
        private static GeoPolygon Square() => new(new[]
        {
            new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10), new GeoPoint(10, 0)
        });

        [Test]
        public void Constructor_DropsARepeatedClosingVertex()
        {
            var closed = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10), new GeoPoint(0, 0)
            });

            Assert.That(closed.Vertices, Has.Count.EqualTo(3));
        }

        [Test]
        public void Constructor_RequiresThreeDistinctVertices()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(() => new GeoPolygon(new[]
                {
                    new GeoPoint(0, 0), new GeoPoint(0, 1)
                }));

                // Three points, but one is just the closing repeat of the first.
                Assert.Throws<ArgumentException>(() => new GeoPolygon(new[]
                {
                    new GeoPoint(0, 0), new GeoPoint(0, 1), new GeoPoint(0, 0)
                }));

                Assert.Throws<ArgumentNullException>(() => new GeoPolygon(null!));
            });
        }

        [Test]
        public void Contains_RespectsHoles()
        {
            var polygon = new GeoPolygon(
                new[] { new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10), new GeoPoint(10, 0) },
                new[]
                {
                    new[] { new GeoPoint(4, 4), new GeoPoint(4, 6), new GeoPoint(6, 6), new GeoPoint(6, 4) }
                });

            Assert.Multiple(() =>
            {
                Assert.That(polygon.Contains(new GeoPoint(1, 1)), Is.True, "Inside the ring.");
                Assert.That(polygon.Contains(new GeoPoint(5, 5)), Is.False, "Inside the hole.");
                Assert.That(polygon.Contains(new GeoPoint(15, 5)), Is.False, "Outside altogether.");
            });
        }

        [Test]
        public void Contains_WorksForAConcavePolygon()
        {
            // An L shape occupying the bottom-left and bottom-right, open at the top middle.
            var polygon = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10),
                new GeoPoint(10, 6), new GeoPoint(4, 6), new GeoPoint(4, 0)
            });

            Assert.Multiple(() =>
            {
                Assert.That(polygon.Contains(new GeoPoint(2, 2)), Is.True);
                Assert.That(polygon.Contains(new GeoPoint(8, 8)), Is.True);
                Assert.That(polygon.Contains(new GeoPoint(8, 2)), Is.False, "In the notch.");
            });
        }

        [Test]
        public void Contains_WorksAcrossTheAntimeridian()
        {
            var polygon = new GeoPolygon(new[]
            {
                new GeoPoint(-1, 179), new GeoPoint(-1, -179),
                new GeoPoint(1, -179), new GeoPoint(1, 179)
            });

            Assert.Multiple(() =>
            {
                Assert.That(polygon.Contains(new GeoPoint(0, 180)), Is.True);
                Assert.That(polygon.Contains(new GeoPoint(0, -179.5)), Is.True);
                Assert.That(polygon.Contains(new GeoPoint(0, 179.5)), Is.True);
                Assert.That(polygon.Contains(new GeoPoint(0, 0)), Is.False);
            });
        }

        [Test]
        public void Area_SubtractsHoles()
        {
            var outer = new[] { new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10), new GeoPoint(10, 0) };
            var hole = new[] { new GeoPoint(4, 4), new GeoPoint(4, 6), new GeoPoint(6, 6), new GeoPoint(6, 4) };

            var solid = new GeoPolygon(outer).Area();
            var holeArea = new GeoPolygon(hole).Area();
            var withHole = new GeoPolygon(outer, new[] { hole }).Area();

            Assert.That(withHole, Is.EqualTo(solid - holeArea).Within(1e-6));
        }

        [Test]
        public void Area_IsIndependentOfWindingDirection()
        {
            var vertices = new[]
            {
                new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10), new GeoPoint(10, 0)
            };

            var clockwise = new GeoPolygon(vertices).Area();
            var counterClockwise = new GeoPolygon(Enumerable.Reverse(vertices)).Area();

            Assert.That(clockwise, Is.EqualTo(counterClockwise).Within(1e-6));
        }

        [Test]
        public void IsClockwise_ReversesWithTheVertexOrder()
        {
            var vertices = new[]
            {
                new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10), new GeoPoint(10, 0)
            };

            Assert.That(new GeoPolygon(vertices).IsClockwise,
                Is.Not.EqualTo(new GeoPolygon(Enumerable.Reverse(vertices)).IsClockwise));
        }

        [Test]
        public void Perimeter_OfTheSquare_IsFourSidesLong()
        {
            var polygon = Square();

            // Two meridian sides of 10 degrees each, plus two parallels that differ in length.
            var expected =
                new GeoPoint(0, 0).DistanceTo(new GeoPoint(0, 10)) +
                new GeoPoint(0, 10).DistanceTo(new GeoPoint(10, 10)) +
                new GeoPoint(10, 10).DistanceTo(new GeoPoint(10, 0)) +
                new GeoPoint(10, 0).DistanceTo(new GeoPoint(0, 0));

            Assert.That(polygon.Perimeter(), Is.EqualTo(expected).Within(1e-6));
        }

        [Test]
        public void TotalBoundaryLength_IncludesHoles()
        {
            var outer = new[] { new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10), new GeoPoint(10, 0) };
            var hole = new[] { new GeoPoint(4, 4), new GeoPoint(4, 6), new GeoPoint(6, 6), new GeoPoint(6, 4) };

            var polygon = new GeoPolygon(outer, new[] { hole });

            Assert.That(polygon.TotalBoundaryLength(), Is.GreaterThan(polygon.Perimeter()));
        }

        [Test]
        public void Centroid_OfASymmetricSquare_IsAtItsMiddle()
        {
            var centroid = Square().Centroid();

            Assert.Multiple(() =>
            {
                Assert.That(centroid.Latitude, Is.EqualTo(5).Within(1e-9));
                Assert.That(centroid.Longitude, Is.EqualTo(5).Within(1e-9));
            });
        }

        [Test]
        public void Centroid_OfASquareIsInsideIt()
        {
            Assert.That(Square().Contains(Square().Centroid()), Is.True);
        }

        [Test]
        public void IsOnBoundary_DetectsPointsOnAnEdge()
        {
            var polygon = Square();

            Assert.Multiple(() =>
            {
                Assert.That(polygon.IsOnBoundary(new GeoPoint(0, 5), 1, DistanceUnit.Meters), Is.True);
                Assert.That(polygon.IsOnBoundary(new GeoPoint(0, 0), 1, DistanceUnit.Meters), Is.True);
                Assert.That(polygon.IsOnBoundary(new GeoPoint(5, 5), 1, DistanceUnit.Meters), Is.False);
            });
        }

        [Test]
        public void Simplify_RemovesCollinearVertices()
        {
            // A triangle with redundant points strung along its edges.
            var polygon = new GeoPolygon(new[]
            {
                new GeoPoint(0, 0), new GeoPoint(0, 2), new GeoPoint(0, 4), new GeoPoint(0, 6),
                new GeoPoint(3, 6), new GeoPoint(6, 6), new GeoPoint(3, 3)
            });

            var simplified = polygon.Simplify(1000, DistanceUnit.Meters);

            Assert.Multiple(() =>
            {
                Assert.That(simplified.Vertices.Count, Is.LessThan(polygon.Vertices.Count));
                Assert.That(simplified.Vertices, Has.Count.GreaterThanOrEqualTo(3));
                Assert.That(simplified.Area(), Is.EqualTo(polygon.Area()).Within(polygon.Area() * 0.02));
            });
        }

        [Test]
        public void Simplify_NeverReducesARingBelowThreeVertices()
        {
            var simplified = Square().Simplify(10_000, DistanceUnit.Kilometers);

            Assert.That(simplified.Vertices, Has.Count.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void Simplify_WithANegativeTolerance_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Square().Simplify(-1));
        }

        [Test]
        public void BoundingBox_EnclosesEveryVertex()
        {
            var polygon = Square();

            Assert.That(polygon.Vertices, Is.All.Matches<GeoPoint>(v => polygon.BoundingBox.Contains(v)));
        }

        [Test]
        public void ToString_ReportsTheVertexAndHoleCounts()
        {
            var polygon = new GeoPolygon(
                new[] { new GeoPoint(0, 0), new GeoPoint(0, 10), new GeoPoint(10, 10), new GeoPoint(10, 0) },
                new[]
                {
                    new[] { new GeoPoint(4, 4), new GeoPoint(4, 6), new GeoPoint(6, 6), new GeoPoint(6, 4) }
                });

            Assert.That(polygon.ToString(), Does.Contain("Vertices: 4").And.Contain("Holes: 1"));
        }
    }
}
