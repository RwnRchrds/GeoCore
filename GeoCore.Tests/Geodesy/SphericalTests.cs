using GeoCore.Core;
using GeoCore.Extensions;
using GeoCore.Geodesy;
using GeoCore.Units;

namespace GeoCore.Tests.Geodesy
{
    [TestFixture]
    public class SphericalTests
    {
        private static readonly GeoPoint London = new(51.5074, -0.1278);
        private static readonly GeoPoint Paris = new(48.8566, 2.3522);
        private static readonly GeoPoint Equator = new(0, 0);

        [Test]
        public void DistanceMeters_AlongTheEquator_MatchesTheSphereCircumference()
        {
            var quarter = Spherical.DistanceMeters(Equator, new GeoPoint(0, 90));
            var expected = 2 * Math.PI * GeoConstants.EarthRadiusMeters / 4;

            Assert.That(quarter, Is.EqualTo(expected).Within(1e-6));
        }

        [Test]
        public void DistanceMeters_BetweenAntipodes_IsHalfTheCircumference()
        {
            var distance = Spherical.DistanceMeters(Equator, Equator.Antipode);

            Assert.That(distance, Is.EqualTo(Math.PI * GeoConstants.EarthRadiusMeters).Within(1e-6));
        }

        [Test]
        public void DistanceMeters_ToItself_IsZero()
        {
            Assert.That(Spherical.DistanceMeters(London, London), Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void DistanceMeters_IsSymmetric()
        {
            Assert.That(Spherical.DistanceMeters(London, Paris),
                Is.EqualTo(Spherical.DistanceMeters(Paris, London)).Within(1e-9));
        }

        [TestCase(0, 1, 0.0)]     // due north
        [TestCase(0, -1, 180.0)]  // due south
        public void InitialBearingDegrees_AlongAMeridian_IsNorthOrSouth(
            double startLatitude, double latitudeDelta, double expected)
        {
            var from = new GeoPoint(startLatitude, 0);
            var to = new GeoPoint(startLatitude + latitudeDelta, 0);

            Assert.That(Spherical.InitialBearingDegrees(from, to), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void InitialBearingDegrees_AlongTheEquator_IsDueEast()
        {
            Assert.That(Spherical.InitialBearingDegrees(Equator, new GeoPoint(0, 1)),
                Is.EqualTo(90).Within(1e-9));
        }

        [Test]
        public void FinalBearingDegrees_EqualsTheReverseOfTheReturnBearing()
        {
            var final = Spherical.FinalBearingDegrees(London, Paris);
            var reverseOfReturn = (Spherical.InitialBearingDegrees(Paris, London) + 180) % 360;

            Assert.That(final, Is.EqualTo(reverseOfReturn).Within(1e-9));
        }

        [Test]
        public void Destination_ThenMeasuringBack_ReturnsTheOriginalDistanceAndBearing()
        {
            var bearing = 37.5;
            var distance = 1234.5 * 1000;

            var destination = Spherical.Destination(London, distance, bearing);

            Assert.Multiple(() =>
            {
                Assert.That(Spherical.DistanceMeters(London, destination), Is.EqualTo(distance).Within(1e-6));
                Assert.That(Spherical.InitialBearingDegrees(London, destination), Is.EqualTo(bearing).Within(1e-9));
            });
        }

        [Test]
        public void Destination_TravellingRightRoundTheWorld_ReturnsToTheStart()
        {
            var circumference = 2 * Math.PI * GeoConstants.EarthRadiusMeters;
            var destination = Spherical.Destination(Equator, circumference, 90);

            Assert.That(Spherical.DistanceMeters(destination, Equator), Is.LessThan(1e-6));
        }

        [Test]
        public void Midpoint_IsEquidistantFromBothEnds()
        {
            var midpoint = Spherical.Midpoint(London, Paris);

            Assert.That(Spherical.DistanceMeters(London, midpoint),
                Is.EqualTo(Spherical.DistanceMeters(Paris, midpoint)).Within(1e-6));
        }

        [Test]
        public void Midpoint_MatchesTheHalfwayIntermediatePoint()
        {
            Assert.That(Spherical.Midpoint(London, Paris),
                Is.EqualTo(Spherical.IntermediatePoint(London, Paris, 0.5)));
        }

        [TestCase(0.0)]
        [TestCase(1.0)]
        public void IntermediatePoint_AtTheEnds_ReturnsTheEndpoints(double fraction)
        {
            var expected = fraction == 0 ? London : Paris;
            var actual = Spherical.IntermediatePoint(London, Paris, fraction);

            Assert.That(Spherical.DistanceMeters(actual, expected), Is.LessThan(1e-6));
        }

        [Test]
        public void IntermediatePoint_DividesTheArcProportionally()
        {
            var total = Spherical.DistanceMeters(London, Paris);
            var quarter = Spherical.IntermediatePoint(London, Paris, 0.25);

            Assert.That(Spherical.DistanceMeters(London, quarter), Is.EqualTo(total * 0.25).Within(1e-6));
        }

        [Test]
        public void IntermediatePoint_ForCoincidentPoints_ReturnsThatPoint()
        {
            Assert.That(Spherical.IntermediatePoint(London, London, 0.5), Is.EqualTo(London));
        }

        [Test]
        public void CrossTrackDistance_MatchesThePublishedWorkedExample()
        {
            // From Chris Veness's "Calculate distance and bearing" reference page.
            var point = new GeoPoint(53.2611, -0.7972);
            var start = new GeoPoint(53.3206, -1.7297);
            var end = new GeoPoint(53.1887, 0.1334);

            Assert.Multiple(() =>
            {
                Assert.That(Spherical.CrossTrackDistanceMeters(point, start, end),
                    Is.EqualTo(-307.5).Within(0.5));
                Assert.That(Spherical.AlongTrackDistanceMeters(point, start, end) / 1000,
                    Is.EqualTo(62.331).Within(0.001));
            });
        }

        [Test]
        public void CrossTrackDistance_ForAPointOnThePath_IsZero()
        {
            var start = new GeoPoint(0, 0);
            var end = new GeoPoint(0, 10);
            var onPath = new GeoPoint(0, 5);

            Assert.That(Spherical.CrossTrackDistanceMeters(onPath, start, end), Is.EqualTo(0).Within(1e-6));
        }

        [Test]
        public void CrossTrackDistance_SignIndicatesWhichSideOfThePath()
        {
            var start = new GeoPoint(0, 0);
            var end = new GeoPoint(0, 10);

            Assert.Multiple(() =>
            {
                Assert.That(Spherical.CrossTrackDistanceMeters(new GeoPoint(1, 5), start, end),
                    Is.Negative, "North of an eastbound path is to the left.");
                Assert.That(Spherical.CrossTrackDistanceMeters(new GeoPoint(-1, 5), start, end),
                    Is.Positive);
            });
        }

        [Test]
        public void AlongTrackDistance_BehindTheStart_IsNegative()
        {
            var start = new GeoPoint(0, 0);
            var end = new GeoPoint(0, 10);

            Assert.That(Spherical.AlongTrackDistanceMeters(new GeoPoint(0, -5), start, end), Is.Negative);
        }

        [Test]
        public void CrossTrackDistance_ForADegeneratePath_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                Spherical.CrossTrackDistanceMeters(Paris, London, London));
        }

        [Test]
        public void RhumbDistance_IsNeverShorterThanTheGreatCircle()
        {
            Assert.That(Spherical.RhumbDistanceMeters(London, Paris),
                Is.GreaterThanOrEqualTo(Spherical.DistanceMeters(London, Paris)));
        }

        [Test]
        public void RhumbDestination_ThenMeasuringBack_ReturnsTheOriginalCourse()
        {
            var bearing = Spherical.RhumbBearingDegrees(London, Paris);
            var distance = Spherical.RhumbDistanceMeters(London, Paris);

            var destination = Spherical.RhumbDestination(London, distance, bearing);

            Assert.That(Spherical.DistanceMeters(destination, Paris), Is.LessThan(0.5));
        }

        [Test]
        public void RhumbBearing_DueEast_StaysDueEastAlongAParallel()
        {
            var from = new GeoPoint(45, 0);
            var to = new GeoPoint(45, 10);

            Assert.That(Spherical.RhumbBearingDegrees(from, to), Is.EqualTo(90).Within(1e-9));
        }

        [Test]
        public void RhumbDestination_AlongAParallel_KeepsTheSameLatitude()
        {
            var from = new GeoPoint(45, 0);
            var destination = Spherical.RhumbDestination(from, 500_000, 90);

            Assert.That(destination.Latitude, Is.EqualTo(45).Within(1e-9));
        }

        [Test]
        public void RhumbMidpoint_IsEquidistantAlongTheRhumbLine()
        {
            var midpoint = Spherical.RhumbMidpoint(London, Paris);

            Assert.That(Spherical.RhumbDistanceMeters(London, midpoint),
                Is.EqualTo(Spherical.RhumbDistanceMeters(midpoint, Paris)).Within(1.0));
        }

        [Test]
        public void RhumbCalculations_WorkAcrossTheAntimeridian()
        {
            var west = new GeoPoint(0, 179);
            var east = new GeoPoint(0, -179);

            // Two degrees apart the short way, not 358 the long way.
            Assert.Multiple(() =>
            {
                Assert.That(Spherical.RhumbDistanceMeters(west, east) / 1000, Is.EqualTo(222.4).Within(1));
                Assert.That(Spherical.RhumbBearingDegrees(west, east), Is.EqualTo(90).Within(1e-9));
            });
        }

        [Test]
        public void DistanceToSegment_ClampsToTheNearestEndpoint()
        {
            var start = new GeoPoint(0, 0);
            var end = new GeoPoint(0, 10);
            var beyond = new GeoPoint(0, 20);

            Assert.That(beyond.DistanceToSegment(start, end, DistanceUnit.Kilometers),
                Is.EqualTo(beyond.DistanceTo(end)).Within(1e-9));
        }

        [Test]
        public void ClosestPointOnSegment_ForAPerpendicularPoint_LandsOnTheSegment()
        {
            var start = new GeoPoint(0, 0);
            var end = new GeoPoint(0, 10);

            var closest = new GeoPoint(1, 5).ClosestPointOnSegment(start, end);

            Assert.Multiple(() =>
            {
                Assert.That(closest.Latitude, Is.EqualTo(0).Within(0.01));
                Assert.That(closest.Longitude, Is.EqualTo(5).Within(0.01));
            });
        }
    }
}
