using System.Globalization;
using GeoCore.Core;
using GeoCore.Extensions;
using GeoCore.Units;
using GeoCore.Units.Conversions;

namespace GeoCore.Tests
{
    /// <summary>
    /// Locks in the behaviour of bugs that were found and fixed, so they cannot come back.
    /// Each test names the defect it guards.
    /// </summary>
    [TestFixture]
    public class RegressionTests
    {
        [Test]
        public void Contains_IsIndependentOfWhichVertexTheRingStartsAt()
        {
            // The ray-casting test used to nudge the caller's point inside the loop, so the
            // answer depended on the order the edges happened to be visited in.
            var vertices = new[]
            {
                new GeoPoint(4, 4), new GeoPoint(2, 0), new GeoPoint(0, 3),
                new GeoPoint(3, 3), new GeoPoint(3, 1)
            };

            var probe = new GeoPoint(3, 2);
            var expected = new GeoPolygon(vertices).Contains(probe);

            for (var rotation = 1; rotation < vertices.Length; rotation++)
            {
                var rotated = vertices.Skip(rotation).Concat(vertices.Take(rotation));

                Assert.That(new GeoPolygon(rotated).Contains(probe), Is.EqualTo(expected),
                    $"Rotating the ring by {rotation} changed the containment result.");
            }
        }

        [Test]
        public void Contains_IsStableAcrossManyRandomRingRotations()
        {
            var random = new Random(42);
            var mismatches = 0;

            for (var trial = 0; trial < 500; trial++)
            {
                var count = random.Next(3, 7);
                var vertices = new List<GeoPoint>();

                // A lattice produces lots of shared latitudes, which is what used to break it.
                for (var i = 0; i < count; i++)
                    vertices.Add(new GeoPoint(random.Next(0, 5), random.Next(0, 5)));

                GeoPolygon baseline;
                try { baseline = new GeoPolygon(vertices); }
                catch (ArgumentException) { continue; }

                var probe = new GeoPoint(random.Next(0, 5), random.Next(0, 5));
                var expected = baseline.Contains(probe);

                for (var rotation = 1; rotation < count; rotation++)
                {
                    var rotated = vertices.Skip(rotation).Concat(vertices.Take(rotation)).ToList();

                    GeoPolygon candidate;
                    try { candidate = new GeoPolygon(rotated); }
                    catch (ArgumentException) { continue; }

                    if (candidate.Contains(probe) != expected)
                        mismatches++;
                }
            }

            Assert.That(mismatches, Is.Zero);
        }

        [Test]
        public void GetBoundingBox_AtThePole_SpansEveryLongitude()
        {
            // Dividing by cos(latitude) used to produce a longitude span of ~1.4e15 degrees.
            var box = new GeoPoint(90, 0).GetBoundingBox(10);

            Assert.Multiple(() =>
            {
                Assert.That(box.MinLongitude, Is.EqualTo(-180));
                Assert.That(box.MaxLongitude, Is.EqualTo(180));
                Assert.That(box.MaxLatitude, Is.EqualTo(90));
            });
        }

        [Test]
        public void GetBoundingBox_NearThePole_DoesNotExceedNinetyDegrees()
        {
            var box = new GeoPoint(89.9, 0).GetBoundingBox(100);

            Assert.That(box.MaxLatitude, Is.LessThanOrEqualTo(90));
        }

        [Test]
        public void GetBoundingBox_NearTheAntimeridian_WrapsAndStillContainsNearbyPoints()
        {
            var fiji = new GeoPoint(-18, 179.9);
            var box = fiji.GetBoundingBox(50);

            // 0.05 degrees east of 179.9 lands at -179.95, on the far side of the dateline.
            Assert.Multiple(() =>
            {
                Assert.That(box.CrossesAntimeridian, Is.True);
                Assert.That(box.Contains(new GeoPoint(-18, -179.95)), Is.True);
                Assert.That(box.Contains(fiji), Is.True);
            });
        }

        [TestCase(0.99999999, "1°00'00.00\"N, 0°00'00.00\"E")]
        [TestCase(51.99999999, "52°00'00.00\"N, 0°00'00.00\"E")]
        public void ToDmsString_NeverEmitsSixtySeconds(double latitude, string expected)
        {
            // Decomposing before rounding used to yield impossible output like 0°59'60.00".
            Assert.That(new GeoPoint(latitude, 0).ToDmsString(), Is.EqualTo(expected));
        }

        [Test]
        public void DistanceConversions_UseExactInternationalDefinitions()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DistanceConverter.Convert(1, DistanceUnit.Miles, DistanceUnit.Kilometers),
                    Is.EqualTo(1.609344));
                Assert.That(DistanceConverter.Convert(1, DistanceUnit.Feet, DistanceUnit.Meters),
                    Is.EqualTo(0.3048));
                Assert.That(DistanceConverter.Convert(1, DistanceUnit.Yards, DistanceUnit.Meters),
                    Is.EqualTo(0.9144));
                Assert.That(DistanceConverter.Convert(1, DistanceUnit.NauticalMiles, DistanceUnit.Meters),
                    Is.EqualTo(1852.0));
                Assert.That(AreaConverter.Convert(1, AreaUnit.Acres, AreaUnit.SquareMeters),
                    Is.EqualTo(4046.8564224));
            });
        }

        [Test]
        public void MoveAlongRoute_WithNegativeDistance_ClampsToTheStart()
        {
            // A negative distance used to satisfy the first segment test immediately and
            // travel backwards off the start of the route.
            var route = new GeoRoute(new[] { new GeoPoint(0, 0), new GeoPoint(0, 2) });

            Assert.That(route.MoveAlongRoute(-500), Is.EqualTo(route.Start));
        }

        [Test]
        public void Area_ForAPolygonStraddlingTheAntimeridian_IsNotWrappedTheLongWayRound()
        {
            // A 2° x 1° box on the equator is roughly 24,700 km²; the unwrapped longitude
            // steps used to measure it as 4.4 million.
            var polygon = new GeoPolygon(new[]
            {
                new GeoPoint(0, 179), new GeoPoint(0, -179),
                new GeoPoint(1, -179), new GeoPoint(1, 179)
            });

            Assert.That(polygon.Area(), Is.EqualTo(24_700).Within(200));
        }

        [Test]
        public void Formatting_DoesNotChangeWithTheAmbientCulture()
        {
            // Everything used to render through the current culture, so a comma-decimal
            // locale produced "51,507400" and broke round-tripping.
            var original = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                var point = new GeoPoint(51.5074, -0.1278);

                Assert.Multiple(() =>
                {
                    Assert.That(point.ToString(),
                        Is.EqualTo("GeoPoint(Latitude: 51.507400, Longitude: -0.127800)"));
                    Assert.That(point.ToDmsString(),
                        Is.EqualTo("51°30'26.64\"N, 0°07'40.08\"W"));
                    Assert.That(GeoPoint.Parse("51.5074, -0.1278"), Is.EqualTo(point));
                });
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void GeoPoint_RejectsImpossibleCoordinates()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(512, 999));
                Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(double.NaN, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(0, double.PositiveInfinity));
                Assert.That(GeoPoint.TryCreate(512, 999, out _), Is.False);
            });
        }

        [Test]
        public void NormalizeLongitude_LeavesAnInRangeValueBitForBitUnchanged()
        {
            // Round-tripping through (x + 180) % 360 - 180 shifted values by an ulp, which was
            // enough to make a bounding box built from a point fail to contain that point.
            foreach (var longitude in new[] { -0.1278, 2.3522, 4.9041, -179.9999, 0.0, 179.9999 })
            {
                Assert.That(AngleConverter.NormalizeLongitude(longitude), Is.EqualTo(longitude),
                    $"NormalizeLongitude perturbed the in-range value {longitude}.");
            }
        }

        [Test]
        public void FromPoints_AlwaysContainsThePointsItWasBuiltFrom()
        {
            var points = new[]
            {
                new GeoPoint(51.5074, -0.1278), new GeoPoint(48.8566, 2.3522), new GeoPoint(52.3676, 4.9041)
            };

            var box = GeoBoundingBox.FromPoints(points);

            Assert.That(points, Is.All.Matches<GeoPoint>(p => box.Contains(p)));
        }

        [Test]
        public void Reversed_DoesNotMutateTheOriginalRoute()
        {
            // Array.Reverse would have reversed the route in place rather than copying it.
            var route = new GeoRoute(new[]
            {
                new GeoPoint(0, 0), new GeoPoint(1, 1), new GeoPoint(2, 2)
            });

            var reversed = route.Reversed();

            Assert.Multiple(() =>
            {
                Assert.That(route.Start, Is.EqualTo(new GeoPoint(0, 0)));
                Assert.That(route.End, Is.EqualTo(new GeoPoint(2, 2)));
                Assert.That(reversed.Start, Is.EqualTo(new GeoPoint(2, 2)));
                Assert.That(reversed.End, Is.EqualTo(new GeoPoint(0, 0)));
            });
        }
    }
}
