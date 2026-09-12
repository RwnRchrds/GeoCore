namespace GeoCore.Units.Conversions
{
    /// <summary>
    /// Provides methods for converting distances between units.
    /// </summary>
    /// <remarks>
    /// All conversions route through metres using the exact international definitions
    /// (1 mile = 1609.344 m, 1 nautical mile = 1852 m, 1 foot = 0.3048 m,
    /// 1 yard = 0.9144 m), so round-tripping a value through any pair of units is
    /// accurate to within floating-point rounding.
    /// </remarks>
    public static class DistanceConverter
    {
        /// <summary>
        /// Returns the exact number of metres in one unit of <paramref name="unit"/>.
        /// </summary>
        /// <param name="unit">The unit to describe.</param>
        /// <returns>The number of metres per unit.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The unit is not a known <see cref="DistanceUnit"/>.</exception>
        public static double MetersPerUnit(DistanceUnit unit) => unit switch
        {
            DistanceUnit.Millimeters => 0.001,
            DistanceUnit.Centimeters => 0.01,
            DistanceUnit.Meters => 1.0,
            DistanceUnit.Kilometers => 1000.0,
            DistanceUnit.Inches => 0.0254,
            DistanceUnit.Feet => 0.3048,
            DistanceUnit.Yards => 0.9144,
            DistanceUnit.Miles => 1609.344,
            DistanceUnit.NauticalMiles => 1852.0,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown distance unit.")
        };

        /// <summary>
        /// Converts a distance in kilometres to the specified unit.
        /// </summary>
        /// <param name="kilometers">The distance in kilometres.</param>
        /// <param name="unit">The unit to convert to.</param>
        /// <returns>The distance expressed in <paramref name="unit"/>.</returns>
        public static double FromKilometers(double kilometers, DistanceUnit unit) =>
            unit == DistanceUnit.Kilometers
                ? kilometers
                : kilometers * 1000.0 / MetersPerUnit(unit);

        /// <summary>
        /// Converts a distance from the specified unit to kilometres.
        /// </summary>
        /// <param name="value">The distance to convert.</param>
        /// <param name="unit">The unit <paramref name="value"/> is expressed in.</param>
        /// <returns>The distance in kilometres.</returns>
        public static double ToKilometers(double value, DistanceUnit unit) =>
            unit == DistanceUnit.Kilometers
                ? value
                : value * MetersPerUnit(unit) / 1000.0;

        /// <summary>
        /// Converts a distance in metres to the specified unit.
        /// </summary>
        /// <param name="meters">The distance in metres.</param>
        /// <param name="unit">The unit to convert to.</param>
        /// <returns>The distance expressed in <paramref name="unit"/>.</returns>
        public static double FromMeters(double meters, DistanceUnit unit) =>
            unit == DistanceUnit.Meters ? meters : meters / MetersPerUnit(unit);

        /// <summary>
        /// Converts a distance from the specified unit to metres.
        /// </summary>
        /// <param name="value">The distance to convert.</param>
        /// <param name="unit">The unit <paramref name="value"/> is expressed in.</param>
        /// <returns>The distance in metres.</returns>
        public static double ToMeters(double value, DistanceUnit unit) =>
            unit == DistanceUnit.Meters ? value : value * MetersPerUnit(unit);

        /// <summary>
        /// Converts a distance value from one unit to another.
        /// </summary>
        /// <param name="value">The distance to convert.</param>
        /// <param name="fromUnit">The unit <paramref name="value"/> is expressed in.</param>
        /// <param name="toUnit">The unit to convert to.</param>
        /// <returns>The converted distance.</returns>
        public static double Convert(double value, DistanceUnit fromUnit, DistanceUnit toUnit)
        {
            if (fromUnit == toUnit)
                return value;

            return value * MetersPerUnit(fromUnit) / MetersPerUnit(toUnit);
        }

        /// <summary>
        /// Returns the conventional abbreviation for a unit, such as <c>km</c> or <c>nmi</c>.
        /// </summary>
        /// <param name="unit">The unit to describe.</param>
        /// <returns>The unit's abbreviation.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The unit is not a known <see cref="DistanceUnit"/>.</exception>
        public static string Abbreviation(DistanceUnit unit) => unit switch
        {
            DistanceUnit.Millimeters => "mm",
            DistanceUnit.Centimeters => "cm",
            DistanceUnit.Meters => "m",
            DistanceUnit.Kilometers => "km",
            DistanceUnit.Inches => "in",
            DistanceUnit.Feet => "ft",
            DistanceUnit.Yards => "yd",
            DistanceUnit.Miles => "mi",
            DistanceUnit.NauticalMiles => "nmi",
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown distance unit.")
        };
    }
}
