using GeoCore.Core;
using GeoCore.Formats;

namespace GeoCore.Tests.Formats
{
    [TestFixture]
    public class CoordinateParserTests
    {
        // Every one of these describes London: 51.5074 N, 0.1278 W.
        [TestCase("51.5074, -0.1278")]
        [TestCase("51.5074,-0.1278")]
        [TestCase("51.5074 -0.1278")]
        [TestCase("+51.5074, -0.1278")]
        [TestCase("51.5074N, 0.1278W")]
        [TestCase("51.5074 N, 0.1278 W")]
        [TestCase("N51.5074 W0.1278")]
        [TestCase("N 51.5074, W 0.1278")]
        [TestCase("51.5074°N, 0.1278°W")]
        [TestCase("0.1278W, 51.5074N")]
        public void TryParse_AcceptsDecimalVariations(string text)
        {
            Assert.That(GeoPoint.TryParse(text, out var point), Is.True, $"Failed to parse '{text}'.");

            Assert.Multiple(() =>
            {
                Assert.That(point.Latitude, Is.EqualTo(51.5074).Within(1e-9));
                Assert.That(point.Longitude, Is.EqualTo(-0.1278).Within(1e-9));
            });
        }

        // The same place again, written sexagesimally: 51°30'26.64"N, 0°07'40.08"W.
        [TestCase("51°30'26.64\"N, 0°07'40.08\"W")]
        [TestCase("51° 30' 26.64\" N, 0° 7' 40.08\" W")]
        [TestCase("51 30 26.64 N, 0 7 40.08 W")]
        [TestCase("51 30 26.64 N 0 7 40.08 W")]
        [TestCase("N51°30'26.64\" W0°07'40.08\"")]
        [TestCase("51d30m26.64s N, 0d7m40.08s W")]
        [TestCase("51°30′26.64″N, 0°07′40.08″W")]
        public void TryParse_AcceptsSexagesimalVariations(string text)
        {
            Assert.That(GeoPoint.TryParse(text, out var point), Is.True, $"Failed to parse '{text}'.");

            Assert.Multiple(() =>
            {
                Assert.That(point.Latitude, Is.EqualTo(51.5074).Within(1e-6));
                Assert.That(point.Longitude, Is.EqualTo(-0.1278).Within(1e-6));
            });
        }

        [Test]
        public void TryParse_HandlesDegreesAndDecimalMinutes()
        {
            // 51°30.444'N, 0°7.668'W
            Assert.That(GeoPoint.TryParse("51 30.444 N, 0 7.668 W", out var point), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(point.Latitude, Is.EqualTo(51.5074).Within(1e-6));
                Assert.That(point.Longitude, Is.EqualTo(-0.1278).Within(1e-6));
            });
        }

        [Test]
        public void Parse_HandlesTheSouthernAndWesternHemispheres()
        {
            var point = GeoPoint.Parse("33°55'29.64\"S, 18°25'26.76\"W");

            Assert.Multiple(() =>
            {
                Assert.That(point.Latitude, Is.EqualTo(-33.9249).Within(1e-6));
                Assert.That(point.Longitude, Is.EqualTo(-18.4241).Within(1e-6));
            });
        }

        [Test]
        public void Parse_HandlesZero()
        {
            Assert.That(GeoPoint.Parse("0, 0"), Is.EqualTo(new GeoPoint(0, 0)));
        }

        [Test]
        public void Parse_RoundTripsTheDmsFormatter()
        {
            var original = new GeoPoint(-33.9249, 18.4241);

            var reparsed = GeoPoint.Parse(original.ToString("DMS"));

            Assert.Multiple(() =>
            {
                Assert.That(reparsed.Latitude, Is.EqualTo(original.Latitude).Within(1e-6));
                Assert.That(reparsed.Longitude, Is.EqualTo(original.Longitude).Within(1e-6));
            });
        }

        [Test]
        public void Parse_RoundTripsTheDecimalFormatter()
        {
            var original = new GeoPoint(51.5074, -0.1278);

            Assert.That(GeoPoint.Parse(original.ToString("D")), Is.EqualTo(original));
        }

        [Test]
        public void Parse_RoundTripsTheDegreesDecimalMinutesFormatter()
        {
            var original = new GeoPoint(51.5074, -0.1278);
            var reparsed = GeoPoint.Parse(original.ToString("DM"));

            Assert.Multiple(() =>
            {
                Assert.That(reparsed.Latitude, Is.EqualTo(original.Latitude).Within(1e-6));
                Assert.That(reparsed.Longitude, Is.EqualTo(original.Longitude).Within(1e-6));
            });
        }

        [TestCase("", Description = "empty")]
        [TestCase("   ", Description = "whitespace")]
        [TestCase("51.5074", Description = "only one value")]
        [TestCase("51.5074, -0.1278, 12", Description = "three values")]
        [TestCase("hello, world", Description = "not numbers")]
        [TestCase("91, 0", Description = "latitude out of range")]
        [TestCase("0, 181", Description = "longitude out of range")]
        [TestCase("51 70 00 N, 0 0 0 W", Description = "minutes of 70")]
        [TestCase("51 30 99 N, 0 0 0 W", Description = "seconds of 99")]
        [TestCase("-51.5 N, 0 W", Description = "sign and hemisphere disagree")]
        [TestCase("51.5 N S, 0 W", Description = "two hemispheres on one value")]
        [TestCase("51.5 30 N, 0 W", Description = "fractional degrees with minutes")]
        [TestCase(", 0", Description = "missing first value")]
        [TestCase("51.5,", Description = "missing second value")]
        public void TryParse_RejectsInvalidInput(string text)
        {
            Assert.That(GeoPoint.TryParse(text, out _), Is.False, $"'{text}' should not have parsed.");
        }

        [Test]
        public void TryParse_WithNull_ReturnsFalse()
        {
            Assert.That(GeoPoint.TryParse(null, out _), Is.False);
        }

        [Test]
        public void Parse_WithNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => GeoPoint.Parse(null!));
        }

        [Test]
        public void Parse_WithInvalidText_ThrowsWithAnExplanation()
        {
            var exception = Assert.Throws<FormatException>(() => GeoPoint.Parse("91, 0"));

            Assert.That(exception!.Message, Does.Contain("out of range"));
        }

        [Test]
        public void Parse_AcceptsTheAntipodalExtremes()
        {
            Assert.Multiple(() =>
            {
                Assert.That(GeoPoint.Parse("90, 180"), Is.EqualTo(new GeoPoint(90, 180)));
                Assert.That(GeoPoint.Parse("-90, -180"), Is.EqualTo(new GeoPoint(-90, -180)));
            });
        }

        [Test]
        public void TryParse_DisambiguatesSecondsFromSouth()
        {
            // 's' hard against a digit means seconds when a hemisphere follows it, and South
            // when one does not.
            Assert.Multiple(() =>
            {
                Assert.That(GeoPoint.Parse("51d30m26.64s N, 0d7m40.08s W").Latitude,
                    Is.EqualTo(51.5074).Within(1e-6), "Seconds marker before a hemisphere.");

                Assert.That(GeoPoint.Parse("33.9249S, 18.4241W").Latitude,
                    Is.EqualTo(-33.9249).Within(1e-9), "South with no hemisphere following.");

                Assert.That(GeoPoint.Parse("33.9249S 18.4241W").Latitude,
                    Is.EqualTo(-33.9249).Within(1e-9), "South followed by the next number.");
            });
        }

        [Test]
        public void CoordinateParser_ExposesTheSameBehaviourAsGeoPoint()
        {
            Assert.Multiple(() =>
            {
                Assert.That(CoordinateParser.Parse("51.5074, -0.1278"),
                    Is.EqualTo(new GeoPoint(51.5074, -0.1278)));
                Assert.That(CoordinateParser.TryParse("nonsense", out _), Is.False);
            });
        }
    }
}
