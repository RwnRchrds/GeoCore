using System.Text;
using GeoCore.Core;

namespace GeoCore.Formats
{
    /// <summary>
    /// Encodes and decodes Google's encoded polyline format, a compact ASCII representation
    /// of a sequence of points.
    /// </summary>
    /// <remarks>
    /// The format stores deltas between consecutive points as base-64 varints, which makes it
    /// far smaller than JSON for a long track. It is lossy: coordinates are rounded to
    /// <c>precision</c> decimal places, 5 by default (about 1&#160;m).
    /// </remarks>
    public static class EncodedPolyline
    {
        /// <summary>
        /// Encodes a sequence of points as an encoded polyline.
        /// </summary>
        /// <param name="points">The points to encode, in order.</param>
        /// <param name="precision">
        /// The number of decimal places to preserve. Google Maps uses 5; some routing engines,
        /// notably OSRM, use 6.
        /// </param>
        /// <returns>The encoded polyline string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="points"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="precision"/> is outside 1–11.</exception>
        public static string Encode(IEnumerable<GeoPoint> points, int precision = 5)
        {
            if (points is null)
                throw new ArgumentNullException(nameof(points));

            if (precision < 1 || precision > 11)
                throw new ArgumentOutOfRangeException(nameof(precision), precision,
                    "Precision must be between 1 and 11 decimal places.");

            var factor = Math.Pow(10, precision);
            var builder = new StringBuilder();

            long previousLatitude = 0;
            long previousLongitude = 0;

            foreach (var point in points)
            {
                var latitude = (long)Math.Round(point.Latitude * factor, MidpointRounding.AwayFromZero);
                var longitude = (long)Math.Round(point.Longitude * factor, MidpointRounding.AwayFromZero);

                // Only the change from the previous point is stored.
                EncodeValue(builder, latitude - previousLatitude);
                EncodeValue(builder, longitude - previousLongitude);

                previousLatitude = latitude;
                previousLongitude = longitude;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Decodes an encoded polyline into its points.
        /// </summary>
        /// <param name="encoded">The encoded polyline string.</param>
        /// <param name="precision">The number of decimal places the polyline was encoded with.</param>
        /// <returns>The decoded points, in order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="encoded"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="precision"/> is outside 1–11.</exception>
        /// <exception cref="FormatException">The string is malformed or decodes to an invalid coordinate.</exception>
        public static IReadOnlyList<GeoPoint> Decode(string encoded, int precision = 5)
        {
            if (encoded is null)
                throw new ArgumentNullException(nameof(encoded));

            if (precision < 1 || precision > 11)
                throw new ArgumentOutOfRangeException(nameof(precision), precision,
                    "Precision must be between 1 and 11 decimal places.");

            var factor = Math.Pow(10, precision);
            var points = new List<GeoPoint>();

            var index = 0;
            long latitude = 0;
            long longitude = 0;

            while (index < encoded.Length)
            {
                latitude += DecodeValue(encoded, ref index);
                longitude += DecodeValue(encoded, ref index);

                var decodedLatitude = latitude / factor;
                var decodedLongitude = longitude / factor;

                if (!GeoPoint.IsValid(decodedLatitude, decodedLongitude))
                    throw new FormatException(FormattableString.Invariant(
                        $"The polyline decoded to an out-of-range coordinate ({decodedLatitude}, {decodedLongitude}). Check the precision matches the encoder's."));

                points.Add(new GeoPoint(decodedLatitude, decodedLongitude));
            }

            return points;
        }

        /// <summary>
        /// Attempts to decode an encoded polyline.
        /// </summary>
        /// <param name="encoded">The encoded polyline string.</param>
        /// <param name="points">Receives the decoded points, or an empty list on failure.</param>
        /// <param name="precision">The number of decimal places the polyline was encoded with.</param>
        /// <returns><c>true</c> if the polyline was decoded successfully.</returns>
        public static bool TryDecode(string? encoded, out IReadOnlyList<GeoPoint> points, int precision = 5)
        {
            points = Array.Empty<GeoPoint>();

            if (encoded is null)
                return false;

            try
            {
                points = Decode(encoded, precision);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private static void EncodeValue(StringBuilder builder, long value)
        {
            // Left-shift by one and invert negatives, so the sign lives in the low bit.
            var shifted = value < 0 ? ~(value << 1) : value << 1;

            while (shifted >= 0x20)
            {
                builder.Append((char)((0x20 | (int)(shifted & 0x1F)) + 63));
                shifted >>= 5;
            }

            builder.Append((char)(shifted + 63));
        }

        private static long DecodeValue(string encoded, ref int index)
        {
            long result = 0;
            var shift = 0;
            int current;

            do
            {
                if (index >= encoded.Length)
                    throw new FormatException("The encoded polyline ended in the middle of a value.");

                current = encoded[index++] - 63;

                if (current < 0)
                    throw new FormatException(
                        $"'{encoded[index - 1]}' is not a valid character in an encoded polyline.");

                result |= (long)(current & 0x1F) << shift;
                shift += 5;

                if (shift > 64)
                    throw new FormatException("The encoded polyline contains an overlong value.");
            }
            while (current >= 0x20);

            // Recover the sign from the low bit.
            return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
        }
    }
}
