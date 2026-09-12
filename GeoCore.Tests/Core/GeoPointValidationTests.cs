using GeoCore.Core;

namespace GeoCore.Tests.Core
{
    [TestFixture]
    public class GeoPointValidationTests
    {
        [TestCase(90.0, 180.0)]
        [TestCase(-90.0, -180.0)]
        [TestCase(0.0, 0.0)]
        public void Constructor_AcceptsTheExtremesOfTheValidRange(double latitude, double longitude)
        {
            Assert.DoesNotThrow(() => new GeoPoint(latitude, longitude));
        }

        [TestCase(90.0001, 0.0)]
        [TestCase(-90.0001, 0.0)]
        [TestCase(0.0, 180.0001)]
        [TestCase(0.0, -180.0001)]
        [TestCase(double.NaN, 0.0)]
        [TestCase(0.0, double.NaN)]
        [TestCase(double.PositiveInfinity, 0.0)]
        [TestCase(0.0, double.NegativeInfinity)]
        public void Constructor_RejectsInvalidCoordinates(double latitude, double longitude)
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(latitude, longitude));
                Assert.That(GeoPoint.IsValid(latitude, longitude), Is.False);
                Assert.That(GeoPoint.TryCreate(latitude, longitude, out _), Is.False);
            });
        }

        [Test]
        public void TryCreate_WithValidInput_ProducesThePoint()
        {
            Assert.Multiple(() =>
            {
                Assert.That(GeoPoint.TryCreate(51.5, -0.12, out var point), Is.True);
                Assert.That(point, Is.EqualTo(new GeoPoint(51.5, -0.12)));
            });
        }

        [Test]
        public void WithExpression_ValidatesTheReplacedValue()
        {
            var point = new GeoPoint(51.5, -0.12);

            Assert.Multiple(() =>
            {
                Assert.That((point with { Latitude = 10 }).Latitude, Is.EqualTo(10));
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = point with { Latitude = 200 });
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = point with { Longitude = 200 });
            });
        }

        [Test]
        public void Clamped_PinsLatitudeAndWrapsLongitude()
        {
            var point = GeoPoint.Clamped(512, 999);

            Assert.Multiple(() =>
            {
                Assert.That(point.Latitude, Is.EqualTo(90), "Latitude clamps at the pole.");
                Assert.That(point.Longitude, Is.EqualTo(-81).Within(1e-9));
            });
        }

        [Test]
        public void Normalized_WrapsLongitudeWithoutTouchingAValidLatitude()
        {
            Assert.Multiple(() =>
            {
                Assert.That(GeoPoint.Normalized(45, 185), Is.EqualTo(new GeoPoint(45, -175)));
                Assert.That(GeoPoint.Normalized(45, -185), Is.EqualTo(new GeoPoint(45, 175)));
                Assert.That(GeoPoint.Normalized(45, 720 + 10), Is.EqualTo(new GeoPoint(45, 10)));
            });
        }

        [Test]
        public void Normalized_TravellingOverAPoleFlipsTheMeridian()
        {
            // 100°N is 80°N on the opposite side of the globe.
            var point = GeoPoint.Normalized(100, 0);

            Assert.Multiple(() =>
            {
                Assert.That(point.Latitude, Is.EqualTo(80).Within(1e-9));
                Assert.That(point.Longitude, Is.EqualTo(180).Within(1e-9).Or.EqualTo(-180).Within(1e-9));
            });
        }

        [Test]
        public void Normalized_AndClamped_RejectNonFiniteValues()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentException>(() => GeoPoint.Normalized(double.NaN, 0));
                Assert.Throws<ArgumentException>(() => GeoPoint.Clamped(0, double.PositiveInfinity));
            });
        }

        [Test]
        public void Antipode_IsHalfAWorldAway()
        {
            var london = new GeoPoint(51.5074, -0.1278);
            var antipode = london.Antipode;

            Assert.Multiple(() =>
            {
                Assert.That(antipode.Latitude, Is.EqualTo(-51.5074).Within(1e-9));
                Assert.That(antipode.Longitude, Is.EqualTo(179.8722).Within(1e-9));

                // Taking the antipode twice returns to the start, up to double rounding:
                // 179.8722 cannot be represented such that subtracting 180 is exact.
                Assert.That(antipode.Antipode.EqualsWithTolerance(london, 1e-12), Is.True);
            });
        }

        [Test]
        public void IsPole_IsTrueOnlyAtTheExtremes()
        {
            Assert.Multiple(() =>
            {
                Assert.That(GeoPoint.NorthPole.IsPole, Is.True);
                Assert.That(GeoPoint.SouthPole.IsPole, Is.True);
                Assert.That(new GeoPoint(89.999, 0).IsPole, Is.False);
            });
        }

        [Test]
        public void Deconstruct_YieldsLatitudeThenLongitude()
        {
            var (latitude, longitude) = new GeoPoint(51.5, -0.12);

            Assert.Multiple(() =>
            {
                Assert.That(latitude, Is.EqualTo(51.5));
                Assert.That(longitude, Is.EqualTo(-0.12));
            });
        }

        [Test]
        public void EqualsWithTolerance_RejectsANegativeTolerance()
        {
            var point = new GeoPoint(0, 0);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => point.EqualsWithTolerance(point, -1));
        }

        [TestCase("G", "GeoPoint(Latitude: 51.507400, Longitude: -0.127800)")]
        [TestCase("D", "51.507400, -0.127800")]
        [TestCase("DMS", "51°30'26.64\"N, 0°07'40.08\"W")]
        [TestCase("DM", "51°30.4440'N, 0°07.6680'W")]
        public void ToString_SupportsTheDocumentedFormats(string format, string expected)
        {
            Assert.That(new GeoPoint(51.5074, -0.1278).ToString(format), Is.EqualTo(expected));
        }

        [Test]
        public void ToString_WithAnUnknownFormat_Throws()
        {
            Assert.Throws<FormatException>(() => new GeoPoint(0, 0).ToString("XYZ"));
        }

        [Test]
        public void ToString_WithNoFormat_MatchesTheGeneralFormat()
        {
            var point = new GeoPoint(51.5074, -0.1278);

            Assert.That(point.ToString(), Is.EqualTo(point.ToString("G")));
        }
    }
}
