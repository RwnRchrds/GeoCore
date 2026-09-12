namespace GeoCore.Core
{
    /// <summary>
    /// Shared geospatial constants.
    /// </summary>
    public static class GeoConstants
    {
        /// <summary>
        /// Mean Earth radius in kilometres (IUGG mean radius R1 of the WGS-84 ellipsoid).
        /// This is the radius used by the spherical (great-circle) calculations.
        /// </summary>
        public const double EarthRadiusKm = 6371.0088;

        /// <summary>
        /// Mean Earth radius in metres.
        /// </summary>
        public const double EarthRadiusMeters = EarthRadiusKm * 1000.0;

        /// <summary>
        /// Length of the WGS-84 semi-major (equatorial) axis, in metres.
        /// </summary>
        public const double Wgs84SemiMajorAxisMeters = 6_378_137.0;

        /// <summary>
        /// Flattening of the WGS-84 ellipsoid (1 / 298.257223563).
        /// </summary>
        public const double Wgs84Flattening = 1.0 / 298.257223563;

        /// <summary>
        /// Length of the WGS-84 semi-minor (polar) axis, in metres.
        /// </summary>
        public const double Wgs84SemiMinorAxisMeters =
            Wgs84SemiMajorAxisMeters * (1.0 - Wgs84Flattening);

        /// <summary>
        /// The minimum valid latitude, in degrees.
        /// </summary>
        public const double MinLatitude = -90.0;

        /// <summary>
        /// The maximum valid latitude, in degrees.
        /// </summary>
        public const double MaxLatitude = 90.0;

        /// <summary>
        /// The minimum valid longitude, in degrees.
        /// </summary>
        public const double MinLongitude = -180.0;

        /// <summary>
        /// The maximum valid longitude, in degrees.
        /// </summary>
        public const double MaxLongitude = 180.0;
    }
}
