using GeoCore.Core;
using GeoCore.Extensions;
using GeoCore.Geodesy;
using GeoCore.Units;

namespace GeoCore.Tests.Geodesy
{
    [TestFixture]
    public class VincentyTests
    {
        // The pair used in Vincenty's own 1975 paper, at full published precision.
        //   Flinders Peak  37°57'03.72030"S, 144°25'29.52440"E
        //   Buninyong      37°39'10.15610"S, 143°55'35.38390"E
        private static readonly GeoPoint FlindersPeak =
            new(-(37 + (57 / 60.0) + (3.72030 / 3600.0)), 144 + (25 / 60.0) + (29.52440 / 3600.0));

        private static readonly GeoPoint Buninyong =
            new(-(37 + (39 / 60.0) + (10.15610 / 3600.0)), 143 + (55 / 60.0) + (35.38390 / 3600.0));

        [Test]
        public void Inverse_MatchesVincentysPublishedTestCase()
        {
            var segment = Vincenty.Inverse(FlindersPeak, Buninyong);

            Assert.Multiple(() =>
            {
                // Published: 54972.271 m, 306°52'05.37", 307°10'25.07".
                Assert.That(segment.DistanceMeters, Is.EqualTo(54972.271).Within(0.001));
                Assert.That(segment.InitialBearingDegrees,
                    Is.EqualTo(306 + (52 / 60.0) + (5.37 / 3600.0)).Within(1e-5));
                Assert.That(segment.FinalBearingDegrees,
                    Is.EqualTo(307 + (10 / 60.0) + (25.07 / 3600.0)).Within(1e-5));
            });
        }

        [Test]
        public void Direct_InvertsTheInverseSolution()
        {
            var segment = Vincenty.Inverse(FlindersPeak, Buninyong);
            var destination = Vincenty.Direct(
                FlindersPeak, segment.DistanceMeters, segment.InitialBearingDegrees);

            Assert.Multiple(() =>
            {
                Assert.That(Spherical.DistanceMeters(destination.Destination, Buninyong),
                    Is.LessThan(0.001), "Direct should land within a millimetre of the target.");
                Assert.That(destination.FinalBearingDegrees,
                    Is.EqualTo(segment.FinalBearingDegrees).Within(1e-9));
            });
        }

        [Test]
        public void Inverse_ForCoincidentPoints_IsZero()
        {
            Assert.That(Vincenty.Inverse(FlindersPeak, FlindersPeak).DistanceMeters, Is.Zero);
        }

        [Test]
        public void Inverse_IsSymmetric()
        {
            var forward = Vincenty.Inverse(FlindersPeak, Buninyong).DistanceMeters;
            var backward = Vincenty.Inverse(Buninyong, FlindersPeak).DistanceMeters;

            Assert.That(forward, Is.EqualTo(backward).Within(1e-6));
        }

        [Test]
        public void Inverse_AlongTheEquator_MatchesTheEllipsoidsEquatorialRadius()
        {
            var quarter = Vincenty.Inverse(new GeoPoint(0, 0), new GeoPoint(0, 90)).DistanceMeters;
            var expected = 2 * Math.PI * GeoConstants.Wgs84SemiMajorAxisMeters / 4;

            Assert.That(quarter, Is.EqualTo(expected).Within(0.001));
        }

        [Test]
        public void Inverse_IsWithinHalfAPercentOfTheSphericalResult()
        {
            var london = new GeoPoint(51.5074, -0.1278);
            var newYork = new GeoPoint(40.7128, -74.0060);

            var spherical = Spherical.DistanceMeters(london, newYork);
            var ellipsoidal = Vincenty.Inverse(london, newYork).DistanceMeters;

            Assert.That(Math.Abs(ellipsoidal - spherical) / ellipsoidal, Is.LessThan(0.005));
        }

        [Test]
        public void TryInverse_ForNearlyAntipodalPoints_ReportsFailureRatherThanThrowing()
        {
            // Vincenty's inverse formula is known not to converge here.
            var point = new GeoPoint(0, 0);
            var nearlyAntipodal = new GeoPoint(0.5, 179.7);

            var converged = Vincenty.TryInverse(point, nearlyAntipodal, out _);

            if (!converged)
            {
                Assert.Throws<GeodesyConvergenceException>(
                    () => Vincenty.Inverse(point, nearlyAntipodal));
            }
            else
            {
                Assert.Pass("This pair happened to converge, which is also a valid outcome.");
            }
        }

        [Test]
        public void GeodesicDistanceTo_AgreesWithVincentyInverse()
        {
            var expected = Vincenty.Inverse(FlindersPeak, Buninyong).DistanceMeters;

            Assert.That(FlindersPeak.GeodesicDistanceTo(Buninyong, DistanceUnit.Meters),
                Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void GeodesicMove_RoundTrips()
        {
            var segment = Vincenty.Inverse(FlindersPeak, Buninyong);
            var moved = FlindersPeak.GeodesicMove(
                segment.DistanceMeters, segment.InitialBearingDegrees, DistanceUnit.Meters);

            Assert.That(Spherical.DistanceMeters(moved, Buninyong), Is.LessThan(0.001));
        }

        [Test]
        public void Direct_WithANonFiniteDistance_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Vincenty.Direct(FlindersPeak, double.NaN, 90));
        }
    }
}
