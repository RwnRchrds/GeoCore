namespace GeoCore.Units.Conversions
{
    /// <summary>
    /// Provides conversion utilities between different area units.
    /// </summary>
    public static class AreaConverter
    {
        /// <summary>
        /// Converts an area value from square kilometers to the specified unit.
        /// </summary>
        /// <param name="km2">The area in square kilometers.</param>
        /// <param name="unit">The target unit to convert to.</param>
        /// <returns>The converted area value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the target unit is not supported.</exception>
        public static double FromSquareKilometers(double km2, AreaUnit unit) => unit switch
        {
            AreaUnit.SquareKilometers => km2,
            AreaUnit.SquareMeters => km2 * 1_000_000,
            AreaUnit.SquareMiles => km2 * 0.386102,
            AreaUnit.Hectares => km2 * 100,
            AreaUnit.Acres => km2 * 247.105,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };

        /// <summary>
        /// Converts an area value to square kilometers from the specified unit.
        /// </summary>
        /// <param name="value">The area value to convert.</param>
        /// <param name="unit">The unit the value is currently in.</param>
        /// <returns>The area in square kilometers.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the source unit is not supported.</exception>
        public static double ToSquareKilometers(double value, AreaUnit unit) => unit switch
        {
            AreaUnit.SquareKilometers => value,
            AreaUnit.SquareMeters => value / 1_000_000,
            AreaUnit.SquareMiles => value / 0.386102,
            AreaUnit.Hectares => value / 100,
            AreaUnit.Acres => value / 247.105,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };

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
            var area = ToSquareKilometers(value, fromUnit);
            return FromSquareKilometers(area, toUnit);
        }
    }
}
