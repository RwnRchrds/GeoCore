using GeoCore.Units.Conversions;

namespace GeoCore.Tests.Converters
{
    public class AngleConverterTests
    {
        [TestCase(0, 0)]
        [TestCase(90, Math.PI / 2)]
        [TestCase(180, Math.PI)]
        [TestCase(360, 2 * Math.PI)]
        public void DegreesToRadians_ReturnsExpected(double degrees, double expected)
        {
            double actual = AngleConverter.DegreesToRadians(degrees);
            Assert.That(actual, Is.EqualTo(expected).Within(1e-6));
        }

        [TestCase(0, 0)]
        [TestCase(Math.PI / 2, 90)]
        [TestCase(Math.PI, 180)]
        [TestCase(2 * Math.PI, 360)]
        public void RadiansToDegrees_ReturnsExpected(double radians, double expected)
        {
            double actual = AngleConverter.RadiansToDegrees(radians);
            Assert.That(actual, Is.EqualTo(expected).Within(1e-6));
        }
    }
}
