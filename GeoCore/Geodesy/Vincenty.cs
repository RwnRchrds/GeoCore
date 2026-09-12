using GeoCore.Core;
using GeoCore.Units.Conversions;

namespace GeoCore.Geodesy
{
    /// <summary>
    /// Geodesic calculations on the WGS-84 ellipsoid using Vincenty's formulae.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are accurate to within about 0.5&#160;mm anywhere on Earth, compared with roughly
    /// 0.5% error for the spherical formulae in <see cref="Spherical"/>. They are
    /// correspondingly slower, since both directions are solved iteratively.
    /// </para>
    /// <para>
    /// Vincenty's inverse formula famously fails to converge for very nearly antipodal
    /// points. <see cref="Inverse"/> throws <see cref="GeodesyConvergenceException"/> in that
    /// case; use <see cref="TryInverse"/> if you would rather detect it and fall back to a
    /// spherical result.
    /// </para>
    /// </remarks>
    public static class Vincenty
    {
        private const int MaxIterations = 200;
        private const double ConvergenceThreshold = 1e-12;

        /// <summary>
        /// Solves the inverse geodesic problem: the distance and azimuths between two points.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <returns>The geodesic joining the two points.</returns>
        /// <exception cref="GeodesyConvergenceException">
        /// The points are so nearly antipodal that the formula does not converge.
        /// </exception>
        public static GeodesicSegment Inverse(GeoPoint from, GeoPoint to)
        {
            if (!TryInverse(from, to, out var result))
            {
                throw new GeodesyConvergenceException(
                    "Vincenty's inverse formula did not converge; the points are very nearly " +
                    "antipodal. Use Spherical.DistanceMeters for an approximate result.");
            }

            return result;
        }

        /// <summary>
        /// Attempts to solve the inverse geodesic problem.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The destination point.</param>
        /// <param name="result">Receives the geodesic, or <c>default</c> if the solution did not converge.</param>
        /// <returns><c>true</c> if a solution was found; otherwise <c>false</c>.</returns>
        public static bool TryInverse(GeoPoint from, GeoPoint to, out GeodesicSegment result)
        {
            result = default;

            const double a = GeoConstants.Wgs84SemiMajorAxisMeters;
            const double f = GeoConstants.Wgs84Flattening;
            const double b = GeoConstants.Wgs84SemiMinorAxisMeters;

            var lat1 = AngleConverter.DegreesToRadians(from.Latitude);
            var lat2 = AngleConverter.DegreesToRadians(to.Latitude);
            var deltaLon = AngleConverter.DegreesToRadians(to.Longitude - from.Longitude);
            deltaLon = GeodesyInternals.WrapLongitudeDelta(deltaLon);

            // Reduced latitudes, i.e. latitudes on the auxiliary sphere.
            var tanU1 = (1 - f) * Math.Tan(lat1);
            var cosU1 = 1 / Math.Sqrt(1 + (tanU1 * tanU1));
            var sinU1 = tanU1 * cosU1;

            var tanU2 = (1 - f) * Math.Tan(lat2);
            var cosU2 = 1 / Math.Sqrt(1 + (tanU2 * tanU2));
            var sinU2 = tanU2 * cosU2;

            var lambda = deltaLon;
            double sinLambda = 0, cosLambda = 0;
            double sinSigma = 0, cosSigma = 0, sigma = 0;
            double cosSquaredAlpha = 0, cos2SigmaM = 0;

            var converged = false;

            for (var iteration = 0; iteration < MaxIterations; iteration++)
            {
                sinLambda = Math.Sin(lambda);
                cosLambda = Math.Cos(lambda);

                var term1 = cosU2 * sinLambda;
                var term2 = (cosU1 * sinU2) - (sinU1 * cosU2 * cosLambda);
                var sinSquaredSigma = (term1 * term1) + (term2 * term2);

                if (sinSquaredSigma < 1e-24)
                {
                    // Coincident points.
                    result = new GeodesicSegment(0, 0, 0);
                    return true;
                }

                sinSigma = Math.Sqrt(sinSquaredSigma);
                cosSigma = (sinU1 * sinU2) + (cosU1 * cosU2 * cosLambda);
                sigma = Math.Atan2(sinSigma, cosSigma);

                var sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
                cosSquaredAlpha = 1 - (sinAlpha * sinAlpha);

                // Zero on an equatorial line, where there is no 2σ_m to speak of.
                cos2SigmaM = cosSquaredAlpha != 0
                    ? cosSigma - (2 * sinU1 * sinU2 / cosSquaredAlpha)
                    : 0;

                var c = f / 16 * cosSquaredAlpha * (4 + (f * (4 - (3 * cosSquaredAlpha))));

                var previousLambda = lambda;
                lambda = deltaLon + ((1 - c) * f * sinAlpha *
                    (sigma + (c * sinSigma *
                        (cos2SigmaM + (c * cosSigma * (-1 + (2 * cos2SigmaM * cos2SigmaM)))))));

                // Antipodal points send lambda off to more than a full turn; give up early.
                if (Math.Abs(lambda) > Math.PI)
                    return false;

                if (Math.Abs(lambda - previousLambda) < ConvergenceThreshold)
                {
                    converged = true;
                    break;
                }
            }

            if (!converged)
                return false;

            var uSquared = cosSquaredAlpha * ((a * a) - (b * b)) / (b * b);

            var aCoefficient = 1 + (uSquared / 16384 *
                (4096 + (uSquared * (-768 + (uSquared * (320 - (175 * uSquared)))))));

            var bCoefficient = uSquared / 1024 *
                (256 + (uSquared * (-128 + (uSquared * (74 - (47 * uSquared))))));

            var deltaSigma = bCoefficient * sinSigma *
                (cos2SigmaM + (bCoefficient / 4 *
                    ((cosSigma * (-1 + (2 * cos2SigmaM * cos2SigmaM))) -
                     (bCoefficient / 6 * cos2SigmaM * (-3 + (4 * sinSigma * sinSigma)) *
                      (-3 + (4 * cos2SigmaM * cos2SigmaM))))));

            var distance = b * aCoefficient * (sigma - deltaSigma);

            var initialBearing = Math.Atan2(
                cosU2 * sinLambda,
                (cosU1 * sinU2) - (sinU1 * cosU2 * cosLambda));

            var finalBearing = Math.Atan2(
                cosU1 * sinLambda,
                (-sinU1 * cosU2) + (cosU1 * sinU2 * cosLambda));

            result = new GeodesicSegment(
                distance,
                AngleConverter.NormalizeDegrees(AngleConverter.RadiansToDegrees(initialBearing)),
                AngleConverter.NormalizeDegrees(AngleConverter.RadiansToDegrees(finalBearing)));

            return true;
        }

        /// <summary>
        /// Solves the direct geodesic problem: where you arrive after travelling a given
        /// distance on a given azimuth.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="distanceMeters">The distance to travel, in metres.</param>
        /// <param name="initialBearingDegrees">The departure azimuth, in degrees clockwise from true north.</param>
        /// <returns>The destination and the azimuth on arrival.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="distanceMeters"/> is not finite.</exception>
        /// <exception cref="GeodesyConvergenceException">The iteration did not converge.</exception>
        public static GeodesicDestination Direct(
            GeoPoint from, double distanceMeters, double initialBearingDegrees)
        {
            if (double.IsNaN(distanceMeters) || double.IsInfinity(distanceMeters))
                throw new ArgumentOutOfRangeException(nameof(distanceMeters), distanceMeters,
                    "Distance must be a finite number.");

            const double a = GeoConstants.Wgs84SemiMajorAxisMeters;
            const double f = GeoConstants.Wgs84Flattening;
            const double b = GeoConstants.Wgs84SemiMinorAxisMeters;

            var lat1 = AngleConverter.DegreesToRadians(from.Latitude);
            var lon1 = AngleConverter.DegreesToRadians(from.Longitude);
            var alpha1 = AngleConverter.DegreesToRadians(initialBearingDegrees);

            var sinAlpha1 = Math.Sin(alpha1);
            var cosAlpha1 = Math.Cos(alpha1);

            var tanU1 = (1 - f) * Math.Tan(lat1);
            var cosU1 = 1 / Math.Sqrt(1 + (tanU1 * tanU1));
            var sinU1 = tanU1 * cosU1;

            var sigma1 = Math.Atan2(tanU1, cosAlpha1);
            var sinAlpha = cosU1 * sinAlpha1;
            var cosSquaredAlpha = 1 - (sinAlpha * sinAlpha);

            var uSquared = cosSquaredAlpha * ((a * a) - (b * b)) / (b * b);

            var aCoefficient = 1 + (uSquared / 16384 *
                (4096 + (uSquared * (-768 + (uSquared * (320 - (175 * uSquared)))))));

            var bCoefficient = uSquared / 1024 *
                (256 + (uSquared * (-128 + (uSquared * (74 - (47 * uSquared))))));

            var sigma = distanceMeters / (b * aCoefficient);
            double sinSigma = 0, cosSigma = 0, cos2SigmaM = 0, deltaSigma;
            var converged = false;

            for (var iteration = 0; iteration < MaxIterations; iteration++)
            {
                cos2SigmaM = Math.Cos((2 * sigma1) + sigma);
                sinSigma = Math.Sin(sigma);
                cosSigma = Math.Cos(sigma);

                deltaSigma = bCoefficient * sinSigma *
                    (cos2SigmaM + (bCoefficient / 4 *
                        ((cosSigma * (-1 + (2 * cos2SigmaM * cos2SigmaM))) -
                         (bCoefficient / 6 * cos2SigmaM * (-3 + (4 * sinSigma * sinSigma)) *
                          (-3 + (4 * cos2SigmaM * cos2SigmaM))))));

                var previousSigma = sigma;
                sigma = (distanceMeters / (b * aCoefficient)) + deltaSigma;

                if (Math.Abs(sigma - previousSigma) < ConvergenceThreshold)
                {
                    converged = true;
                    break;
                }
            }

            if (!converged)
                throw new GeodesyConvergenceException(
                    "Vincenty's direct formula did not converge.");

            var x = (sinU1 * sinSigma) - (cosU1 * cosSigma * cosAlpha1);

            var lat2 = Math.Atan2(
                (sinU1 * cosSigma) + (cosU1 * sinSigma * cosAlpha1),
                (1 - f) * Math.Sqrt((sinAlpha * sinAlpha) + (x * x)));

            var lambda = Math.Atan2(
                sinSigma * sinAlpha1,
                (cosU1 * cosSigma) - (sinU1 * sinSigma * cosAlpha1));

            var c = f / 16 * cosSquaredAlpha * (4 + (f * (4 - (3 * cosSquaredAlpha))));

            var l = lambda - ((1 - c) * f * sinAlpha *
                (sigma + (c * sinSigma *
                    (cos2SigmaM + (c * cosSigma * (-1 + (2 * cos2SigmaM * cos2SigmaM)))))));

            var finalBearing = Math.Atan2(sinAlpha, -x);

            return new GeodesicDestination(
                GeodesyInternals.PointFromRadians(lat2, lon1 + l),
                AngleConverter.NormalizeDegrees(AngleConverter.RadiansToDegrees(finalBearing)));
        }
    }
}
