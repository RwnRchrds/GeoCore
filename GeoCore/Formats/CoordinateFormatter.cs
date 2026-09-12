using System.Globalization;
using System.Text;

namespace GeoCore.Formats
{
    /// <summary>
    /// Formats latitude and longitude values as human-readable sexagesimal strings.
    /// </summary>
    /// <remarks>
    /// All methods default to <see cref="CultureInfo.InvariantCulture"/> so that output does
    /// not change with the ambient locale — a coordinate rendered as <c>51,5074</c> in a
    /// comma-decimal locale is not round-trippable and is rarely what callers want.
    /// </remarks>
    public static class CoordinateFormatter
    {
        /// <summary>
        /// Formats a latitude and longitude pair as degrees, minutes and seconds.
        /// </summary>
        /// <param name="latitude">The latitude in degrees.</param>
        /// <param name="longitude">The longitude in degrees.</param>
        /// <param name="secondsDecimals">The number of decimal places to show on the seconds.</param>
        /// <param name="formatProvider">The number format provider. Defaults to the invariant culture.</param>
        /// <returns>A string such as <c>51°30'26.64"N, 0°07'40.08"W</c>.</returns>
        public static string ToDms(
            double latitude,
            double longitude,
            int secondsDecimals = 2,
            IFormatProvider? formatProvider = null) =>
            LatitudeToDms(latitude, secondsDecimals, formatProvider) + ", " +
            LongitudeToDms(longitude, secondsDecimals, formatProvider);

        /// <summary>
        /// Formats a latitude as degrees, minutes and seconds with an N/S suffix.
        /// </summary>
        /// <param name="latitude">The latitude in degrees.</param>
        /// <param name="secondsDecimals">The number of decimal places to show on the seconds.</param>
        /// <param name="formatProvider">The number format provider. Defaults to the invariant culture.</param>
        /// <returns>A string such as <c>51°30'26.64"N</c>.</returns>
        public static string LatitudeToDms(
            double latitude,
            int secondsDecimals = 2,
            IFormatProvider? formatProvider = null) =>
            FormatDms(latitude, latitude < 0 ? 'S' : 'N', secondsDecimals, formatProvider);

        /// <summary>
        /// Formats a longitude as degrees, minutes and seconds with an E/W suffix.
        /// </summary>
        /// <param name="longitude">The longitude in degrees.</param>
        /// <param name="secondsDecimals">The number of decimal places to show on the seconds.</param>
        /// <param name="formatProvider">The number format provider. Defaults to the invariant culture.</param>
        /// <returns>A string such as <c>0°07'40.08"W</c>.</returns>
        public static string LongitudeToDms(
            double longitude,
            int secondsDecimals = 2,
            IFormatProvider? formatProvider = null) =>
            FormatDms(longitude, longitude < 0 ? 'W' : 'E', secondsDecimals, formatProvider);

        /// <summary>
        /// Formats a latitude and longitude pair as degrees and decimal minutes, the
        /// convention used by most marine and aviation equipment.
        /// </summary>
        /// <param name="latitude">The latitude in degrees.</param>
        /// <param name="longitude">The longitude in degrees.</param>
        /// <param name="minuteDecimals">The number of decimal places to show on the minutes.</param>
        /// <param name="formatProvider">The number format provider. Defaults to the invariant culture.</param>
        /// <returns>A string such as <c>51°30.4440'N, 0°07.6680'W</c>.</returns>
        public static string ToDegreesDecimalMinutes(
            double latitude,
            double longitude,
            int minuteDecimals = 4,
            IFormatProvider? formatProvider = null) =>
            FormatDegreesDecimalMinutes(latitude, latitude < 0 ? 'S' : 'N', minuteDecimals, formatProvider) + ", " +
            FormatDegreesDecimalMinutes(longitude, longitude < 0 ? 'W' : 'E', minuteDecimals, formatProvider);

        /// <summary>
        /// Splits a signed decimal degree value into whole degrees, whole minutes and seconds.
        /// </summary>
        /// <param name="decimalDegrees">The value to decompose. The sign is discarded.</param>
        /// <param name="secondsDecimals">
        /// The precision the seconds will ultimately be displayed at. Rounding is applied
        /// before the split so that the result can never carry into an invalid value such as
        /// 60 seconds.
        /// </param>
        /// <returns>The absolute value expressed as degrees, minutes and seconds.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="secondsDecimals"/> is negative or greater than 15, or
        /// <paramref name="decimalDegrees"/> is not finite.
        /// </exception>
        public static (int Degrees, int Minutes, double Seconds) Decompose(
            double decimalDegrees,
            int secondsDecimals = 2)
        {
            if (double.IsNaN(decimalDegrees) || double.IsInfinity(decimalDegrees))
                throw new ArgumentOutOfRangeException(nameof(decimalDegrees), decimalDegrees,
                    "Value must be a finite number.");

            if (secondsDecimals < 0 || secondsDecimals > 15)
                throw new ArgumentOutOfRangeException(nameof(secondsDecimals), secondsDecimals,
                    "Decimal places must be between 0 and 15.");

            // Round to the display precision *first*. Decomposing and then rounding is what
            // produces impossible output like 0°59'60.00" for 0.99999999 degrees.
            var totalSeconds = Math.Round(
                Math.Abs(decimalDegrees) * 3600.0, secondsDecimals, MidpointRounding.AwayFromZero);

            var degrees = (int)Math.Floor(totalSeconds / 3600.0);
            var remainder = totalSeconds - (degrees * 3600.0);

            var minutes = (int)Math.Floor(remainder / 60.0);
            var seconds = Math.Round(remainder - (minutes * 60.0), secondsDecimals, MidpointRounding.AwayFromZero);

            // Belt and braces: mop up any floating-point residue left by the subtractions.
            if (seconds >= 60.0)
            {
                seconds -= 60.0;
                minutes++;
            }

            if (minutes >= 60)
            {
                minutes -= 60;
                degrees++;
            }

            return (degrees, minutes, seconds);
        }

        private static string FormatDms(
            double value, char hemisphere, int secondsDecimals, IFormatProvider? formatProvider)
        {
            var provider = formatProvider ?? CultureInfo.InvariantCulture;
            var (degrees, minutes, seconds) = Decompose(value, secondsDecimals);

            var builder = new StringBuilder();
            builder.Append(degrees.ToString("0", provider));
            builder.Append('°');
            builder.Append(minutes.ToString("00", provider));
            builder.Append('\'');
            builder.Append(seconds.ToString(FixedFormat("00", secondsDecimals), provider));
            builder.Append('"');
            builder.Append(hemisphere);

            return builder.ToString();
        }

        private static string FormatDegreesDecimalMinutes(
            double value, char hemisphere, int minuteDecimals, IFormatProvider? formatProvider)
        {
            var provider = formatProvider ?? CultureInfo.InvariantCulture;

            if (minuteDecimals < 0 || minuteDecimals > 15)
                throw new ArgumentOutOfRangeException(nameof(minuteDecimals), minuteDecimals,
                    "Decimal places must be between 0 and 15.");

            var totalMinutes = Math.Round(
                Math.Abs(value) * 60.0, minuteDecimals, MidpointRounding.AwayFromZero);

            var degrees = (int)Math.Floor(totalMinutes / 60.0);
            var minutes = Math.Round(totalMinutes - (degrees * 60.0), minuteDecimals, MidpointRounding.AwayFromZero);

            if (minutes >= 60.0)
            {
                minutes -= 60.0;
                degrees++;
            }

            return degrees.ToString("0", provider) + '°' +
                   minutes.ToString(FixedFormat("00", minuteDecimals), provider) + '\'' + hemisphere;
        }

        private static string FixedFormat(string integerPart, int decimals) =>
            decimals <= 0 ? integerPart : integerPart + "." + new string('0', decimals);
    }
}
