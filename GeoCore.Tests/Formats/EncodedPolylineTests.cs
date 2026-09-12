using GeoCore.Core;
using GeoCore.Formats;

namespace GeoCore.Tests.Formats
{
    [TestFixture]
    public class EncodedPolylineTests
    {
        // The example published in Google's encoded polyline specification.
        private const string GoogleExample = "_p~iF~ps|U_ulLnnqC_mqNvxq`@";

        private static readonly GeoPoint[] GooglePoints =
        {
            new(38.5, -120.2),
            new(40.7, -120.95),
            new(43.252, -126.453)
        };

        [Test]
        public void Encode_MatchesTheGoogleSpecificationExample()
        {
            Assert.That(EncodedPolyline.Encode(GooglePoints), Is.EqualTo(GoogleExample));
        }

        [Test]
        public void Decode_MatchesTheGoogleSpecificationExample()
        {
            Assert.That(EncodedPolyline.Decode(GoogleExample), Is.EqualTo(GooglePoints));
        }

        [Test]
        public void EncodeThenDecode_RoundTripsWithinThePrecision()
        {
            var points = new[]
            {
                new GeoPoint(51.50735, -0.12776),
                new GeoPoint(48.85661, 2.35222),
                new GeoPoint(-33.86882, 151.20930)
            };

            var decoded = EncodedPolyline.Decode(EncodedPolyline.Encode(points));

            Assert.That(decoded, Is.EqualTo(points));
        }

        [Test]
        public void EncodeThenDecode_AtPrecisionSix_RoundTrips()
        {
            // OSRM and some other routing engines use six decimal places.
            var points = new[] { new GeoPoint(51.507351, -0.127758), new GeoPoint(48.856614, 2.352222) };

            var decoded = EncodedPolyline.Decode(EncodedPolyline.Encode(points, 6), 6);

            Assert.That(decoded, Is.EqualTo(points));
        }

        [Test]
        public void Encode_AnEmptySequence_ProducesAnEmptyString()
        {
            Assert.That(EncodedPolyline.Encode(Array.Empty<GeoPoint>()), Is.Empty);
        }

        [Test]
        public void Decode_AnEmptyString_ProducesNoPoints()
        {
            Assert.That(EncodedPolyline.Decode(string.Empty), Is.Empty);
        }

        [Test]
        public void Encode_HandlesAllFourQuadrants()
        {
            var points = new[]
            {
                new GeoPoint(10, 10), new GeoPoint(-10, 10),
                new GeoPoint(-10, -10), new GeoPoint(10, -10)
            };

            Assert.That(EncodedPolyline.Decode(EncodedPolyline.Encode(points)), Is.EqualTo(points));
        }

        [Test]
        public void Decode_WithAMismatchedPrecision_FailsLoudly()
        {
            // Decoding six-figure data as five-figure inflates the coordinates out of range.
            var encoded = EncodedPolyline.Encode(new[] { new GeoPoint(51.5, -0.12) }, 6);

            Assert.That(EncodedPolyline.TryDecode(encoded, out _), Is.False);
        }

        [Test]
        public void Decode_WithATruncatedValue_Throws()
        {
            Assert.Throws<FormatException>(() => EncodedPolyline.Decode("_p~iF~ps|U_ulL"));
        }

        [TestCase(0)]
        [TestCase(12)]
        public void Encode_WithAnInvalidPrecision_Throws(int precision)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => EncodedPolyline.Encode(GooglePoints, precision));
        }

        [Test]
        public void Encode_WithANullSequence_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => EncodedPolyline.Encode(null!));
        }
    }
}
