using System.Text;
using GeoCore.Core;

namespace GeoCore.Formats
{
    /// <summary>
    /// The four compass directions used when walking between neighbouring geohash cells.
    /// </summary>
    public enum GeohashDirection
    {
        /// <summary>Towards the north pole.</summary>
        North,

        /// <summary>Towards the south pole.</summary>
        South,

        /// <summary>Towards increasing longitude.</summary>
        East,

        /// <summary>Towards decreasing longitude.</summary>
        West
    }

    /// <summary>
    /// Encodes and decodes geohashes: short strings that name a rectangular cell of the
    /// globe, where a shared prefix means physical proximity.
    /// </summary>
    /// <remarks>
    /// Geohashes are widely used as database keys for proximity search, because a prefix
    /// query selects a whole region. Note the classic caveat: two nearby points can have
    /// completely different geohashes if they straddle a cell boundary, which is what
    /// <see cref="Neighbors"/> is for.
    /// </remarks>
    public static class Geohash
    {
        private const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";

        private static readonly int[] Bits = { 16, 8, 4, 2, 1 };

        private static readonly string[] NeighborNorth =
            { "p0r21436x8zb9dcf5h7kjnmqesgutwvy", "bc01fg45238967deuvhjyznpkmstqrwx" };

        private static readonly string[] NeighborSouth =
            { "14365h7k9dcfesgujnmqp0r2twvyx8zb", "238967debc01fg45kmstqrwxuvhjyznp" };

        private static readonly string[] NeighborEast =
            { "bc01fg45238967deuvhjyznpkmstqrwx", "p0r21436x8zb9dcf5h7kjnmqesgutwvy" };

        private static readonly string[] NeighborWest =
            { "238967debc01fg45kmstqrwxuvhjyznp", "14365h7k9dcfesgujnmqp0r2twvyx8zb" };

        private static readonly char[][] BorderNorth =
            { "prxz".ToCharArray(), "bcfguvyz".ToCharArray() };

        private static readonly char[][] BorderSouth =
            { "028b".ToCharArray(), "0145hjnp".ToCharArray() };

        private static readonly char[][] BorderEast =
            { "bcfguvyz".ToCharArray(), "prxz".ToCharArray() };

        private static readonly char[][] BorderWest =
            { "0145hjnp".ToCharArray(), "028b".ToCharArray() };

        /// <summary>
        /// Encodes a point as a geohash.
        /// </summary>
        /// <param name="point">The point to encode.</param>
        /// <param name="precision">
        /// The number of characters to produce, between 1 and 12. Each character narrows the
        /// cell: 5 characters is roughly 5&#160;km across, 8 is roughly 40&#160;m, 12 is under
        /// 4&#160;cm.
        /// </param>
        /// <returns>The geohash string.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="precision"/> is outside 1–12.</exception>
        public static string Encode(GeoPoint point, int precision = 9)
        {
            if (precision < 1 || precision > 12)
                throw new ArgumentOutOfRangeException(nameof(precision), precision,
                    "Precision must be between 1 and 12 characters.");

            var minLatitude = GeoConstants.MinLatitude;
            var maxLatitude = GeoConstants.MaxLatitude;
            var minLongitude = GeoConstants.MinLongitude;
            var maxLongitude = GeoConstants.MaxLongitude;

            var builder = new StringBuilder(precision);
            var isLongitudeTurn = true;
            var bit = 0;
            var value = 0;

            while (builder.Length < precision)
            {
                // Bits alternate between longitude and latitude, longitude first.
                if (isLongitudeTurn)
                {
                    var mid = (minLongitude + maxLongitude) / 2;
                    if (point.Longitude >= mid)
                    {
                        value |= Bits[bit];
                        minLongitude = mid;
                    }
                    else
                    {
                        maxLongitude = mid;
                    }
                }
                else
                {
                    var mid = (minLatitude + maxLatitude) / 2;
                    if (point.Latitude >= mid)
                    {
                        value |= Bits[bit];
                        minLatitude = mid;
                    }
                    else
                    {
                        maxLatitude = mid;
                    }
                }

                isLongitudeTurn = !isLongitudeTurn;

                if (bit < 4)
                {
                    bit++;
                }
                else
                {
                    builder.Append(Base32[value]);
                    bit = 0;
                    value = 0;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Decodes a geohash to the cell it names.
        /// </summary>
        /// <param name="geohash">The geohash to decode.</param>
        /// <returns>The bounding box of the cell.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="geohash"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="geohash"/> is empty or contains an invalid character.</exception>
        public static GeoBoundingBox DecodeToBoundingBox(string geohash)
        {
            if (geohash is null)
                throw new ArgumentNullException(nameof(geohash));

            if (geohash.Length == 0)
                throw new FormatException("A geohash must contain at least one character.");

            var minLatitude = GeoConstants.MinLatitude;
            var maxLatitude = GeoConstants.MaxLatitude;
            var minLongitude = GeoConstants.MinLongitude;
            var maxLongitude = GeoConstants.MaxLongitude;

            var isLongitudeTurn = true;

            foreach (var character in geohash)
            {
                var value = Base32.IndexOf(char.ToLowerInvariant(character));

                if (value < 0)
                    throw new FormatException(
                        $"'{character}' is not a valid geohash character. Geohashes use base-32 without a, i, l or o.");

                foreach (var mask in Bits)
                {
                    var isSet = (value & mask) != 0;

                    if (isLongitudeTurn)
                    {
                        var mid = (minLongitude + maxLongitude) / 2;
                        if (isSet)
                            minLongitude = mid;
                        else
                            maxLongitude = mid;
                    }
                    else
                    {
                        var mid = (minLatitude + maxLatitude) / 2;
                        if (isSet)
                            minLatitude = mid;
                        else
                            maxLatitude = mid;
                    }

                    isLongitudeTurn = !isLongitudeTurn;
                }
            }

            return new GeoBoundingBox(minLatitude, minLongitude, maxLatitude, maxLongitude);
        }

        /// <summary>
        /// Decodes a geohash to the centre of the cell it names.
        /// </summary>
        /// <param name="geohash">The geohash to decode.</param>
        /// <returns>The centre point of the cell.</returns>
        /// <remarks>
        /// A geohash names a rectangle rather than a point, so this is only as precise as the
        /// hash is long. Use <see cref="DecodeToBoundingBox"/> if you need to know how much
        /// slack there is.
        /// </remarks>
        public static GeoPoint Decode(string geohash) => DecodeToBoundingBox(geohash).Center;

        /// <summary>
        /// Attempts to decode a geohash to the centre of its cell.
        /// </summary>
        /// <param name="geohash">The geohash to decode.</param>
        /// <param name="point">Receives the decoded point, or <c>default</c> on failure.</param>
        /// <returns><c>true</c> if the geohash was valid.</returns>
        public static bool TryDecode(string? geohash, out GeoPoint point)
        {
            point = default;

            if (string.IsNullOrEmpty(geohash))
                return false;

            foreach (var character in geohash!)
            {
                if (Base32.IndexOf(char.ToLowerInvariant(character)) < 0)
                    return false;
            }

            point = Decode(geohash);
            return true;
        }

        /// <summary>
        /// Returns the geohash of the cell adjacent to this one in the given direction.
        /// </summary>
        /// <param name="geohash">The starting geohash.</param>
        /// <param name="direction">The direction to step in.</param>
        /// <returns>The neighbouring cell's geohash, at the same precision.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="geohash"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="geohash"/> is empty or invalid.</exception>
        public static string Adjacent(string geohash, GeohashDirection direction)
        {
            if (geohash is null)
                throw new ArgumentNullException(nameof(geohash));

            if (geohash.Length == 0)
                throw new FormatException("A geohash must contain at least one character.");

            var normalized = geohash.ToLowerInvariant();
            var lastCharacter = normalized[normalized.Length - 1];
            var parent = normalized.Substring(0, normalized.Length - 1);

            // Even- and odd-length hashes split latitude and longitude the opposite way round.
            var parity = normalized.Length % 2;

            var (neighbors, border) = direction switch
            {
                GeohashDirection.North => (NeighborNorth[parity], BorderNorth[parity]),
                GeohashDirection.South => (NeighborSouth[parity], BorderSouth[parity]),
                GeohashDirection.East => (NeighborEast[parity], BorderEast[parity]),
                GeohashDirection.West => (NeighborWest[parity], BorderWest[parity]),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction.")
            };

            // Stepping off the edge of the parent cell means the parent must move too.
            if (Array.IndexOf(border, lastCharacter) >= 0 && parent.Length > 0)
                parent = Adjacent(parent, direction);

            var index = neighbors.IndexOf(lastCharacter);

            if (index < 0)
                throw new FormatException(
                    $"'{lastCharacter}' is not a valid geohash character. Geohashes use base-32 without a, i, l or o.");

            return parent + Base32[index];
        }

        /// <summary>
        /// Returns the eight geohash cells surrounding this one, plus the cell itself.
        /// </summary>
        /// <param name="geohash">The centre geohash.</param>
        /// <returns>
        /// The nine cells of the 3×3 block centred on <paramref name="geohash"/>, starting
        /// with the centre cell itself.
        /// </returns>
        /// <remarks>
        /// Searching the full 3×3 block is the standard way to avoid missing results that sit
        /// just over a cell boundary from the point you are searching around.
        /// </remarks>
        public static IReadOnlyList<string> Neighbors(string geohash)
        {
            var north = Adjacent(geohash, GeohashDirection.North);
            var south = Adjacent(geohash, GeohashDirection.South);

            return new[]
            {
                geohash,
                north,
                Adjacent(north, GeohashDirection.East),
                Adjacent(geohash, GeohashDirection.East),
                Adjacent(south, GeohashDirection.East),
                south,
                Adjacent(south, GeohashDirection.West),
                Adjacent(geohash, GeohashDirection.West),
                Adjacent(north, GeohashDirection.West)
            };
        }
    }
}
