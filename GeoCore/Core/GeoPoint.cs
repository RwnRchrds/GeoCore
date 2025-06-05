namespace GeoCore.Core
{
    /// <summary>
    /// Represents a geographical point with latitude and longitude.
    /// </summary>
    public readonly record struct GeoPoint(double Latitude, double Longitude)
    {
        public override string ToString()
        {
            return $"GeoPoint(Latitude: {Latitude:F6}, Longitude: {Longitude:F6})";
        }

        public bool EqualsWithTolerance(GeoPoint other, double tolerance = 1e-6)
        {
            return Math.Abs(Latitude - other.Latitude) < tolerance &&
                   Math.Abs(Longitude - other.Longitude) < tolerance;
        }
    }
}
