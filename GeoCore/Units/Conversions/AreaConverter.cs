namespace GeoCore.Units.Conversions
{
    /// <summary>
    /// Provides conversion utilities between different area units.
    /// </summary>
    /// <remarks>
    /// All conversions route through square metres using exact definitions derived from
    /// the international foot and mile, so round-tripping is accurate to within
    /// floating-point rounding.
    /// </remarks>
    public static class AreaConverter
    {
        /// <summary>
        /// Returns the exact number of square metres in one unit of <paramref name="unit"/>.
        /// </summary>
        /// <param name="unit">The unit to describe.</param>
        /// <returns>The number of square metres per unit.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The unit is not a known <see cref="AreaUnit"/>.</exception>
        public static double SquareMetersPerUnit(AreaUnit unit) => unit switch
        {
            AreaUnit.SquareMeters => 1.0,
            AreaUnit.SquareKilometers => 1_000_000.0,
            AreaUnit.SquareMiles => 1609.344 * 1609.344,   // 2,589,988.110336
            AreaUnit.Hectares => 10_000.0,
            AreaUnit.Acres => 4046.8564224,
            AreaUnit.SquareFeet => 0.3048 * 0.3048,        // 0.09290304
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown area unit.")
        };

        /// <summary>
        /// Converts an area value from square kilometres to the specified unit.
        /// </summary>
        /// <param name="km2">The area in square kilometres.</param>
        /// <param name="unit">The target unit to convert to.</param>
        /// <returns>The converted area value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the target unit is not supported.</exception>
        public static double FromSquareKilometers(double km2, AreaUnit unit) =>
            unit == AreaUnit.SquareKilometers
                ? km2
                : km2 * 1_000_000.0 / SquareMetersPerUnit(unit);

        /// <summary>
        /// Converts an area value to square kilometres from the specified unit.
        /// </summary>
        /// <param name="value">The area value to convert.</param>
        /// <param name="unit">The unit the value is currently in.</param>
        /// <returns>The area in square kilometres.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the source unit is not supported.</exception>
        public static double ToSquareKilometers(double value, AreaUnit unit) =>
            unit == AreaUnit.SquareKilometers
                ? value
                : value * SquareMetersPerUnit(unit) / 1_000_000.0;

        /// <summary>
        /// Converts an area value from square metres to the specified unit.
        /// </summary>
        /// <param name="squareMeters">The area in square metres.</param>
        /// <param name="unit">The target unit to convert to.</param>
        /// <returns>The converted area value.</returns>
        public static double FromSquareMeters(double squareMeters, AreaUnit unit) =>
            unit == AreaUnit.SquareMeters ? squareMeters : squareMeters / SquareMetersPerUnit(unit);

        /// <summary>
        /// Converts an area value to square metres from the specified unit.
        /// </summary>
        /// <param name="value">The area value to convert.</param>
        /// <param name="unit">The unit the value is currently in.</param>
        /// <returns>The area in square metres.</returns>
        public static double ToSquareMeters(double value, AreaUnit unit) =>
            unit == AreaUnit.SquareMeters ? value : value * SquareMetersPerUnit(unit);

        /// <summary>
        /// Converts an area value from one unit to another.
        /// </summary>
        /// <param name="value">The area value to convert.</param>
        /// <param name="fromUnit">The unit the value is currently in.</param>
        /// <param name="toUnit">The unit to convert to.</param>
        /// <returns>The converted area value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when either the source or target unit is not supported.
        /// </exception>
        public static double Convert(double value, AreaUnit fromUnit, AreaUnit toUnit)
        {
            if (fromUnit == toUnit)
                return value;

            return value * SquareMetersPerUnit(fromUnit) / SquareMetersPerUnit(toUnit);
        }
    }
}
