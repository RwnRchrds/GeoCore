using System.Globalization;
using GeoCore.Compatibility;
using GeoCore.Formats;
using GeoCore.Units.Conversions;

namespace GeoCore.Core
{
    /// <summary>
    /// Represents a geographical point with latitude and longitude, in degrees, on the WGS-84 datum.
    /// </summary>
    /// <remarks>
    /// A <see cref="GeoPoint"/> is always a valid location: latitude is constrained to
    /// [-90, 90], longitude to [-180, 180], and neither may be NaN or infinite. Construction
    /// from out-of-range values throws. To accept untrusted input, use
    /// <see cref="TryCreate(double, double, out GeoPoint)"/>, <see cref="Clamped"/> or
    /// <see cref="Normalized"/>.
    /// </remarks>
    public readonly record struct GeoPoint : IFormattable
    {
        private readonly double _latitude;
        private readonly double _longitude;

        /// <summary>
        /// Initializes a new <see cref="GeoPoint"/>.
        /// </summary>
        /// <param name="latitude">The latitude in degrees, between -90 and 90 inclusive.</param>
        /// <param name="longitude">The longitude in degrees, between -180 and 180 inclusive.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="latitude"/> or <paramref name="longitude"/> is outside its valid
        /// range, or is NaN or infinite.
        /// </exception>
        public GeoPoint(double latitude, double longitude)
        {
            _latitude = ValidateLatitude(latitude, nameof(latitude));
            _longitude = ValidateLongitude(longitude, nameof(longitude));
        }

        /// <summary>
        /// Gets the latitude in degrees, between -90 (south pole) and 90 (north pole).
        /// </summary>
        public double Latitude
        {
            get => _latitude;
            init => _latitude = ValidateLatitude(value, nameof(value));
        }

        /// <summary>
        /// Gets the longitude in degrees, between -180 and 180. Negative values are west
        /// of the prime meridian.
        /// </summary>
        public double Longitude
        {
            get => _longitude;
            init => _longitude = ValidateLongitude(value, nameof(value));
        }

        /// <summary>
        /// Deconstructs the point into its latitude and longitude.
        /// </summary>
        /// <param name="latitude">Receives the latitude in degrees.</param>
        /// <param name="longitude">Receives the longitude in degrees.</param>
        public void Deconstruct(out double latitude, out double longitude)
        {
            latitude = _latitude;
            longitude = _longitude;
        }

        /// <summary>
        /// The point at latitude 0, longitude 0, in the Gulf of Guinea.
        /// </summary>
        public static GeoPoint Origin => new(0, 0);

        /// <summary>
        /// The geographic north pole.
        /// </summary>
        public static GeoPoint NorthPole => new(90, 0);

        /// <summary>
        /// The geographic south pole.
        /// </summary>
        public static GeoPoint SouthPole => new(-90, 0);

        /// <summary>
        /// Gets a value indicating whether this point lies at one of the geographic poles,
        /// where longitude is not meaningful.
        /// </summary>
        public bool IsPole => Math.Abs(_latitude) >= 90.0;

        /// <summary>
        /// Gets the point diametrically opposite this one on the globe.
        /// </summary>
        public GeoPoint Antipode =>
            new(-_latitude, AngleConverter.NormalizeLongitude(_longitude + 180.0));

        /// <summary>
        /// Determines whether the given values describe a valid latitude/longitude pair.
        /// </summary>
        /// <param name="latitude">The latitude in degrees.</param>
        /// <param name="longitude">The longitude in degrees.</param>
        /// <returns><c>true</c> if a <see cref="GeoPoint"/> could be constructed from these values.</returns>
        public static bool IsValid(double latitude, double longitude) =>
            IsFinite(latitude) && IsFinite(longitude) &&
            latitude >= GeoConstants.MinLatitude && latitude <= GeoConstants.MaxLatitude &&
            longitude >= GeoConstants.MinLongitude && longitude <= GeoConstants.MaxLongitude;

        /// <summary>
        /// Attempts to create a <see cref="GeoPoint"/>, returning <c>false</c> instead of
        /// throwing when the coordinates are out of range.
        /// </summary>
        /// <param name="latitude">The latitude in degrees.</param>
        /// <param name="longitude">The longitude in degrees.</param>
        /// <param name="point">Receives the created point, or <c>default</c> on failure.</param>
        /// <returns><c>true</c> if the point was created; otherwise <c>false</c>.</returns>
        public static bool TryCreate(double latitude, double longitude, out GeoPoint point)
        {
            if (!IsValid(latitude, longitude))
            {
                point = default;
                return false;
            }

            point = new GeoPoint(latitude, longitude);
            return true;
        }

        /// <summary>
        /// Creates a point by clamping latitude into [-90, 90] and wrapping longitude into
        /// [-180, 180).
        /// </summary>
        /// <param name="latitude">The latitude in degrees.</param>
        /// <param name="longitude">The longitude in degrees.</param>
        /// <returns>A valid <see cref="GeoPoint"/>.</returns>
        /// <exception cref="ArgumentException">Either value is NaN or infinite.</exception>
        /// <remarks>
        /// This is usually what you want for sanitising input: a latitude beyond the pole is
        /// almost always a data error rather than a journey over the pole. Use
        /// <see cref="Normalized"/> if you genuinely want to wrap over the pole.
        /// </remarks>
        public static GeoPoint Clamped(double latitude, double longitude)
        {
            RequireFinite(latitude, nameof(latitude));
            RequireFinite(longitude, nameof(longitude));

            return new GeoPoint(
                MathCompat.Clamp(latitude, GeoConstants.MinLatitude, GeoConstants.MaxLatitude),
                AngleConverter.NormalizeLongitude(longitude));
        }

        /// <summary>
        /// Creates a point by wrapping out-of-range coordinates around the globe.
        /// </summary>
        /// <param name="latitude">The latitude in degrees, possibly outside [-90, 90].</param>
        /// <param name="longitude">The longitude in degrees, possibly outside [-180, 180].</param>
        /// <returns>A valid <see cref="GeoPoint"/> describing the same physical location.</returns>
        /// <exception cref="ArgumentException">Either value is NaN or infinite.</exception>
        /// <remarks>
        /// Latitude wraps as though travelling along a meridian: passing over a pole reflects
        /// the latitude and shifts the longitude by 180°. For example, a latitude of 100° is
        /// 80° on the opposite meridian.
        /// </remarks>
        public static GeoPoint Normalized(double latitude, double longitude)
        {
            RequireFinite(latitude, nameof(latitude));
            RequireFinite(longitude, nameof(longitude));

            var lat = latitude % 360.0;
            if (lat < 0)
                lat += 360.0;

            var flipLongitude = false;

            if (lat > 270.0)
            {
                lat -= 360.0;                 // (270, 360) -> (-90, 0)
            }
            else if (lat > 90.0)
            {
                lat = 180.0 - lat;            // (90, 270] -> [-90, 90), over a pole
                flipLongitude = true;
            }

            var lon = flipLongitude ? longitude + 180.0 : longitude;

            return new GeoPoint(
                MathCompat.Clamp(lat, GeoConstants.MinLatitude, GeoConstants.MaxLatitude),
                AngleConverter.NormalizeLongitude(lon));
        }

        /// <summary>
        /// Determines whether this point is within <paramref name="tolerance"/> degrees of
        /// another on both axes.
        /// </summary>
        /// <param name="other">The point to compare against.</param>
        /// <param name="tolerance">The maximum permitted difference in degrees on either axis.</param>
        /// <returns><c>true</c> if the points are within tolerance of one another.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="tolerance"/> is negative or NaN.</exception>
        /// <remarks>
        /// This compares raw degrees, so the ground distance a given tolerance represents
        /// shrinks with longitude as you approach the poles. Compare distances with
        /// <c>DistanceTo</c> if you need a metric threshold.
        /// </remarks>
        public bool EqualsWithTolerance(GeoPoint other, double tolerance = 1e-6)
        {
            if (double.IsNaN(tolerance) || tolerance < 0)
                throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance,
                    "Tolerance must be a non-negative number.");

            return Math.Abs(_latitude - other._latitude) < tolerance &&
                   Math.Abs(_longitude - other._longitude) < tolerance;
        }

        /// <inheritdoc />
        public override string ToString() => ToString(null, null);

        /// <summary>
        /// Formats the point using the specified format string.
        /// </summary>
        /// <param name="format">
        /// One of: <c>G</c> (default, descriptive), <c>D</c> (decimal degrees),
        /// <c>DMS</c> (degrees/minutes/seconds), or <c>DM</c> (degrees and decimal minutes).
        /// </param>
        /// <returns>The formatted point.</returns>
        public string ToString(string? format) => ToString(format, null);

        /// <summary>
        /// Formats the point using the specified format string and format provider.
        /// </summary>
        /// <param name="format">
        /// One of: <c>G</c> (default, descriptive), <c>D</c> (decimal degrees),
        /// <c>DMS</c> (degrees/minutes/seconds), or <c>DM</c> (degrees and decimal minutes).
        /// </param>
        /// <param name="formatProvider">
        /// The provider used to format the numbers. Defaults to the invariant culture so that
        /// output is stable across locales.
        /// </param>
        /// <returns>The formatted point.</returns>
        /// <exception cref="FormatException">The format string is not recognised.</exception>
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            var provider = formatProvider ?? CultureInfo.InvariantCulture;

            if (string.IsNullOrEmpty(format))
                format = "G";

            switch (format!.ToUpperInvariant())
            {
                case "G":
                    return string.Format(provider,
                        "GeoPoint(Latitude: {0:F6}, Longitude: {1:F6})", _latitude, _longitude);

                case "D":
                    return string.Format(provider, "{0:F6}, {1:F6}", _latitude, _longitude);

                case "DMS":
                    return CoordinateFormatter.ToDms(_latitude, _longitude, 2, provider);

                case "DM":
                    return CoordinateFormatter.ToDegreesDecimalMinutes(_latitude, _longitude, 4, provider);

                default:
                    throw new FormatException(
                        $"The '{format}' format string is not supported by GeoPoint. Use G, D, DMS or DM.");
            }
        }

        /// <summary>
        /// Parses a coordinate string in decimal or degrees/minutes/seconds form.
        /// </summary>
        /// <param name="value">
        /// The text to parse, for example <c>"51.5074, -0.1278"</c>,
        /// <c>"51°30'26.6\"N, 0°7'40.1\"W"</c> or <c>"N51 30 26.6 W0 7 40.1"</c>.
        /// </param>
        /// <returns>The parsed point.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The text could not be parsed as a coordinate pair.</exception>
        public static GeoPoint Parse(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (!CoordinateParser.TryParse(value, out var point, out var error))
                throw new FormatException(error);

            return point;
        }

        /// <summary>
        /// Attempts to parse a coordinate string in decimal or degrees/minutes/seconds form.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <param name="point">Receives the parsed point, or <c>default</c> on failure.</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
        public static bool TryParse(string? value, out GeoPoint point) =>
            CoordinateParser.TryParse(value, out point, out _);

        private static double ValidateLatitude(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(paramName, value,
                    "Latitude must be a finite number.");

            if (value < GeoConstants.MinLatitude || value > GeoConstants.MaxLatitude)
                throw new ArgumentOutOfRangeException(paramName, value,
                    FormattableString.Invariant(
                        $"Latitude must be between {GeoConstants.MinLatitude} and {GeoConstants.MaxLatitude} degrees."));

            return value;
        }

        private static double ValidateLongitude(double value, string paramName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(paramName, value,
                    "Longitude must be a finite number.");

            if (value < GeoConstants.MinLongitude || value > GeoConstants.MaxLongitude)
                throw new ArgumentOutOfRangeException(paramName, value,
                    FormattableString.Invariant(
                        $"Longitude must be between {GeoConstants.MinLongitude} and {GeoConstants.MaxLongitude} degrees."));

            return value;
        }

        private static bool IsFinite(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value);

        private static void RequireFinite(double value, string paramName)
        {
            if (!IsFinite(value))
                throw new ArgumentException("Value must be a finite number.", paramName);
        }
    }
}
