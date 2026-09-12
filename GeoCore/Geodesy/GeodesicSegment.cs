using GeoCore.Core;

namespace GeoCore.Geodesy
{
    /// <summary>
    /// The result of solving the geodesic inverse problem: the shortest path between two
    /// points on the ellipsoid.
    /// </summary>
    /// <param name="DistanceMeters">The length of the geodesic, in metres.</param>
    /// <param name="InitialBearingDegrees">The azimuth on departure, in degrees clockwise from true north.</param>
    /// <param name="FinalBearingDegrees">The azimuth on arrival, in degrees clockwise from true north.</param>
    public readonly record struct GeodesicSegment(
        double DistanceMeters,
        double InitialBearingDegrees,
        double FinalBearingDegrees);

    /// <summary>
    /// The result of solving the geodesic direct problem: where you arrive after travelling a
    /// given distance on a given azimuth.
    /// </summary>
    /// <param name="Destination">The point reached.</param>
    /// <param name="FinalBearingDegrees">The azimuth on arrival, in degrees clockwise from true north.</param>
    public readonly record struct GeodesicDestination(
        GeoPoint Destination,
        double FinalBearingDegrees);
}
