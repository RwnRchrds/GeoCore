using GeoCore.Units;
using GeoCore.Units.Conversions;

namespace GeoCore.Tests.Converters
{
    public class DistanceConverterTests
    {
        [TestCase(1, DistanceUnit.Kilometers, DistanceUnit.Meters, 1000)]
        [TestCase(1, DistanceUnit.Kilometers, DistanceUnit.Miles, 0.621371)]
        [TestCase(1, DistanceUnit.Miles, DistanceUnit.Kilometers, 1.60934)]
        [TestCase(5280, DistanceUnit.Feet, DistanceUnit.Miles, 1)]
        public void Convert_BetweenUnits_ReturnsExpected(double value, DistanceUnit from, DistanceUnit to, double expected)
        {
            double actual = DistanceConverter.Convert(value, from, to);
            Assert.That(actual, Is.EqualTo(expected).Within(1e-4));
        }
    }
}