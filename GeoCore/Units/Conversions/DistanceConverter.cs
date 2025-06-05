namespace GeoCore.Units.Conversions
{
    /// <summary>
    /// Provides methods for converting distances between units.
    /// </summary>
    public static class DistanceConverter
    {
        /// <summary>
        /// Converts a distance in kilometers to the specified unit.
        /// </summary>
        public static double FromKilometers(double kilometers, DistanceUnit unit) => unit switch
        {
            DistanceUnit.Kilometers => kilometers,
            DistanceUnit.Meters => kilometers * 1_000,
            DistanceUnit.Miles => kilometers * 0.621371,
            DistanceUnit.NauticalMiles => kilometers * 0.539957,
            DistanceUnit.Feet => kilometers * 3_280.84,
            DistanceUnit.Yards => kilometers * 1_093.61,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };

        /// <summary>
        /// Converts a distance from the specified unit to kilometers.
        /// </summary>
        public static double ToKilometers(double value, DistanceUnit unit) => unit switch
        {
            DistanceUnit.Kilometers => value,
            DistanceUnit.Meters => value / 1_000,
            DistanceUnit.Miles => value / 0.621371,
            DistanceUnit.NauticalMiles => value / 0.539957,
            DistanceUnit.Feet => value / 3_280.84,
            DistanceUnit.Yards => value / 1_093.61,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };

        /// <summary>
        /// Converts a distance value from one unit to another.
        /// </summary>
        public static double Convert(double value, DistanceUnit fromUnit, DistanceUnit toUnit)
        {
            if (fromUnit == toUnit)
                return value;

            var valueInKm = ToKilometers(value, fromUnit);
            return FromKilometers(valueInKm, toUnit);
        }
    }
}
