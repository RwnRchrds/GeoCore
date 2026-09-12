using GeoCore.Units;
using GeoCore.Units.Conversions;

namespace GeoCore.Tests.Converters
{
    [TestFixture]
    public class UnitConversionTests
    {
        [Test]
        public void DistanceConvert_RoundTripsThroughEveryUnitPair()
        {
            var units = (DistanceUnit[])Enum.GetValues(typeof(DistanceUnit));

            foreach (var from in units)
            {
                foreach (var to in units)
                {
                    var there = DistanceConverter.Convert(1234.5, from, to);
                    var back = DistanceConverter.Convert(there, to, from);

                    Assert.That(back, Is.EqualTo(1234.5).Within(1e-9),
                        $"Round trip {from} -> {to} -> {from} lost precision.");
                }
            }
        }

        [Test]
        public void AreaConvert_RoundTripsThroughEveryUnitPair()
        {
            var units = (AreaUnit[])Enum.GetValues(typeof(AreaUnit));

            foreach (var from in units)
            {
                foreach (var to in units)
                {
                    var there = AreaConverter.Convert(1234.5, from, to);
                    var back = AreaConverter.Convert(there, to, from);

                    Assert.That(back, Is.EqualTo(1234.5).Within(1e-9),
                        $"Round trip {from} -> {to} -> {from} lost precision.");
                }
            }
        }

        [TestCase(DistanceUnit.Kilometers, 1000.0)]
        [TestCase(DistanceUnit.Meters, 1.0)]
        [TestCase(DistanceUnit.Miles, 1609.344)]
        [TestCase(DistanceUnit.NauticalMiles, 1852.0)]
        [TestCase(DistanceUnit.Feet, 0.3048)]
        [TestCase(DistanceUnit.Yards, 0.9144)]
        [TestCase(DistanceUnit.Inches, 0.0254)]
        [TestCase(DistanceUnit.Centimeters, 0.01)]
        [TestCase(DistanceUnit.Millimeters, 0.001)]
        public void MetersPerUnit_MatchesTheInternationalDefinition(DistanceUnit unit, double expected)
        {
            Assert.That(DistanceConverter.MetersPerUnit(unit), Is.EqualTo(expected));
        }

        [Test]
        public void DistanceRelationships_HoldExactly()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DistanceConverter.Convert(5280, DistanceUnit.Feet, DistanceUnit.Miles),
                    Is.EqualTo(1).Within(1e-12), "5280 feet make a mile.");
                Assert.That(DistanceConverter.Convert(3, DistanceUnit.Feet, DistanceUnit.Yards),
                    Is.EqualTo(1).Within(1e-12), "3 feet make a yard.");
                Assert.That(DistanceConverter.Convert(12, DistanceUnit.Inches, DistanceUnit.Feet),
                    Is.EqualTo(1).Within(1e-12), "12 inches make a foot.");
            });
        }

        [Test]
        public void AreaRelationships_HoldExactly()
        {
            Assert.Multiple(() =>
            {
                Assert.That(AreaConverter.Convert(100, AreaUnit.Hectares, AreaUnit.SquareKilometers),
                    Is.EqualTo(1).Within(1e-12), "100 hectares make a square kilometre.");
                Assert.That(AreaConverter.Convert(640, AreaUnit.Acres, AreaUnit.SquareMiles),
                    Is.EqualTo(1).Within(1e-12), "640 acres make a square mile.");
                Assert.That(AreaConverter.SquareMetersPerUnit(AreaUnit.SquareMiles),
                    Is.EqualTo(1609.344 * 1609.344));
            });
        }

        [Test]
        public void Converters_RejectUnknownUnits()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => DistanceConverter.MetersPerUnit((DistanceUnit)99));
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => AreaConverter.SquareMetersPerUnit((AreaUnit)99));
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => DistanceConverter.Abbreviation((DistanceUnit)99));
            });
        }

        [Test]
        public void Abbreviation_ReturnsTheConventionalSymbol()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DistanceConverter.Abbreviation(DistanceUnit.Kilometers), Is.EqualTo("km"));
                Assert.That(DistanceConverter.Abbreviation(DistanceUnit.NauticalMiles), Is.EqualTo("nmi"));
                Assert.That(DistanceConverter.Abbreviation(DistanceUnit.Feet), Is.EqualTo("ft"));
            });
        }

        [TestCase(370.0, 10.0)]
        [TestCase(-10.0, 350.0)]
        [TestCase(720.0, 0.0)]
        [TestCase(-370.0, 350.0)]
        public void NormalizeDegrees_WrapsIntoZeroToThreeSixty(double input, double expected)
        {
            Assert.That(AngleConverter.NormalizeDegrees(input), Is.EqualTo(expected).Within(1e-9));
        }

        [TestCase(190.0, -170.0)]
        [TestCase(-190.0, 170.0)]
        [TestCase(180.0, -180.0)]
        [TestCase(-180.0, -180.0)]
        [TestCase(0.0, 0.0)]
        public void NormalizeLongitude_WrapsIntoPlusOrMinusOneEighty(double input, double expected)
        {
            Assert.That(AngleConverter.NormalizeLongitude(input), Is.EqualTo(expected).Within(1e-9));
        }

        [TestCase(10.0, 20.0, 10.0)]
        [TestCase(350.0, 10.0, 20.0)]
        [TestCase(10.0, 350.0, -20.0)]
        [TestCase(0.0, 180.0, 180.0)]
        public void DifferenceBetweenBearings_ReturnsTheShortestTurn(
            double from, double to, double expected)
        {
            Assert.That(AngleConverter.DifferenceBetweenBearings(from, to),
                Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void DegreesAndRadians_RoundTrip()
        {
            foreach (var degrees in new[] { 0.0, 45.0, 90.0, 180.0, -37.5, 359.999 })
            {
                Assert.That(
                    AngleConverter.RadiansToDegrees(AngleConverter.DegreesToRadians(degrees)),
                    Is.EqualTo(degrees).Within(1e-12));
            }
        }
    }
}
