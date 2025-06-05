namespace GeoCore.Core
{
    /// <summary>
    /// Represents a rectangular bounding box defined by minimum and maximum latitude and longitude.
    /// </summary>
    public readonly record struct GeoBoundingBox(
        double MinLatitude,
        double MinLongitude,
        double MaxLatitude,
        double MaxLongitude)
    {
        public override string ToString() =>
            $"BoundingBox(Lat: {MinLatitude:F6} to {MaxLatitude:F6}, Lon: {MinLongitude:F6} to {MaxLongitude:F6})";

        /// <summary>
        /// Checks whether a given GeoPoint falls within the bounding box.
        /// </summary>
        public bool Contains(GeoPoint point) =>
            point.Latitude >= MinLatitude && point.Latitude <= MaxLatitude &&
            point.Longitude >= MinLongitude && point.Longitude <= MaxLongitude;
    }
}
