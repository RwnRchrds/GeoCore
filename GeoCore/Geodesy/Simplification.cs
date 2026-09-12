using GeoCore.Core;
using GeoCore.Extensions;
using GeoCore.Units;

namespace GeoCore.Geodesy
{
    /// <summary>
    /// Line-simplification algorithms shared by routes and polygon rings.
    /// </summary>
    internal static class Simplification
    {
        /// <summary>
        /// Reduces a sequence of points using the Ramer-Douglas-Peucker algorithm, keeping
        /// every point that lies further than <paramref name="toleranceMeters"/> from the
        /// simplified line.
        /// </summary>
        /// <remarks>
        /// Implemented with an explicit stack rather than recursion so that very long GPS
        /// tracks cannot overflow it.
        /// </remarks>
        internal static List<GeoPoint> DouglasPeucker(IReadOnlyList<GeoPoint> points, double toleranceMeters)
        {
            if (points.Count <= 2 || toleranceMeters <= 0)
                return new List<GeoPoint>(points);

            var keep = new bool[points.Count];
            keep[0] = true;
            keep[points.Count - 1] = true;

            var pending = new Stack<(int First, int Last)>();
            pending.Push((0, points.Count - 1));

            while (pending.Count > 0)
            {
                var (first, last) = pending.Pop();

                if (last <= first + 1)
                    continue;

                var farthestIndex = -1;
                var farthestDistance = toleranceMeters;

                for (var i = first + 1; i < last; i++)
                {
                    var distance = points[i].DistanceToSegment(
                        points[first], points[last], DistanceUnit.Meters);

                    if (distance > farthestDistance)
                    {
                        farthestDistance = distance;
                        farthestIndex = i;
                    }
                }

                if (farthestIndex < 0)
                    continue;

                keep[farthestIndex] = true;
                pending.Push((first, farthestIndex));
                pending.Push((farthestIndex, last));
            }

            var result = new List<GeoPoint>();
            for (var i = 0; i < points.Count; i++)
            {
                if (keep[i])
                    result.Add(points[i]);
            }

            return result;
        }
    }
}
