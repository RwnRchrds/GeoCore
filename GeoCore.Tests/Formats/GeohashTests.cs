using GeoCore.Core;
using GeoCore.Formats;

namespace GeoCore.Tests.Formats
{
    [TestFixture]
    public class GeohashTests
    {
        [Test]
        public void Encode_MatchesTheCanonicalWorkedExample()
        {
            // 57.64911, 10.40744 is the example used in the geohash literature.
            Assert.That(Geohash.Encode(new GeoPoint(57.64911, 10.40744), 11),
                Is.EqualTo("u4pruydqqvj"));
        }

        [Test]
        public void Decode_ReturnsAPointInsideTheEncodedCell()
        {
            var original = new GeoPoint(51.5074, -0.1278);
            var hash = Geohash.Encode(original, 9);

            var cell = Geohash.DecodeToBoundingBox(hash);

            Assert.Multiple(() =>
            {
                Assert.That(cell.Contains(original), Is.True);
                Assert.That(Geohash.Decode(hash).Latitude, Is.EqualTo(original.Latitude).Within(0.001));
                Assert.That(Geohash.Decode(hash).Longitude, Is.EqualTo(original.Longitude).Within(0.001));
            });
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(9)]
        [TestCase(12)]
        public void Decode_IsAlwaysInsideTheCellItNames(int precision)
        {
            var point = new GeoPoint(-33.8688, 151.2093);
            var hash = Geohash.Encode(point, precision);

            Assert.That(Geohash.DecodeToBoundingBox(hash).Contains(point), Is.True);
        }

        [Test]
        public void Encode_LongerHashesAreMorePrecise()
        {
            var point = new GeoPoint(51.5074, -0.1278);

            var coarse = Geohash.DecodeToBoundingBox(Geohash.Encode(point, 4));
            var fine = Geohash.DecodeToBoundingBox(Geohash.Encode(point, 8));

            Assert.That(fine.LatitudeSpan, Is.LessThan(coarse.LatitudeSpan));
        }

        [Test]
        public void Encode_NearbyPointsShareAPrefix()
        {
            var a = Geohash.Encode(new GeoPoint(51.5074, -0.1278), 7);
            var b = Geohash.Encode(new GeoPoint(51.5075, -0.1279), 7);

            Assert.That(a.Substring(0, 5), Is.EqualTo(b.Substring(0, 5)));
        }

        [Test]
        public void Encode_ShorterHashIsAPrefixOfTheLongerOne()
        {
            var point = new GeoPoint(35.6895, 139.6917);

            Assert.That(Geohash.Encode(point, 9), Does.StartWith(Geohash.Encode(point, 5)));
        }

        [TestCase(0)]
        [TestCase(13)]
        public void Encode_WithAnInvalidPrecision_Throws(int precision)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Geohash.Encode(new GeoPoint(0, 0), precision));
        }

        [Test]
        public void Decode_WithAnInvalidCharacter_Throws()
        {
            // 'a', 'i', 'l' and 'o' are excluded from the geohash alphabet.
            Assert.Throws<FormatException>(() => Geohash.Decode("u4pria"));
        }

        [Test]
        public void TryDecode_WithInvalidInput_ReturnsFalse()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Geohash.TryDecode("abc", out _), Is.False);
                Assert.That(Geohash.TryDecode("", out _), Is.False);
                Assert.That(Geohash.TryDecode(null, out _), Is.False);
                Assert.That(Geohash.TryDecode("u4pruyd", out _), Is.True);
            });
        }

        [Test]
        public void Adjacent_StepsIntoTheNeighbouringCell()
        {
            var hash = Geohash.Encode(new GeoPoint(51.5074, -0.1278), 7);
            var cell = Geohash.DecodeToBoundingBox(hash);

            var north = Geohash.DecodeToBoundingBox(Geohash.Adjacent(hash, GeohashDirection.North));
            var east = Geohash.DecodeToBoundingBox(Geohash.Adjacent(hash, GeohashDirection.East));

            Assert.Multiple(() =>
            {
                Assert.That(north.MinLatitude, Is.EqualTo(cell.MaxLatitude).Within(1e-9));
                Assert.That(east.MinLongitude, Is.EqualTo(cell.MaxLongitude).Within(1e-9));
            });
        }

        [Test]
        public void Adjacent_NorthThenSouth_ReturnsToTheOriginalCell()
        {
            var hash = Geohash.Encode(new GeoPoint(48.8566, 2.3522), 8);

            var roundTrip = Geohash.Adjacent(
                Geohash.Adjacent(hash, GeohashDirection.North), GeohashDirection.South);

            Assert.That(roundTrip, Is.EqualTo(hash));
        }

        [Test]
        public void Adjacent_EastThenWest_ReturnsToTheOriginalCell()
        {
            var hash = Geohash.Encode(new GeoPoint(48.8566, 2.3522), 8);

            var roundTrip = Geohash.Adjacent(
                Geohash.Adjacent(hash, GeohashDirection.East), GeohashDirection.West);

            Assert.That(roundTrip, Is.EqualTo(hash));
        }

        [Test]
        public void Neighbors_ReturnsNineDistinctCellsIncludingTheCentre()
        {
            var hash = Geohash.Encode(new GeoPoint(51.5074, -0.1278), 7);
            var neighbors = Geohash.Neighbors(hash);

            Assert.Multiple(() =>
            {
                Assert.That(neighbors, Has.Count.EqualTo(9));
                Assert.That(neighbors, Has.Member(hash));
                Assert.That(neighbors.Distinct().Count(), Is.EqualTo(9));
                Assert.That(neighbors, Is.All.Length.EqualTo(hash.Length));
            });
        }

        [Test]
        public void Neighbors_SurroundTheCentreCell()
        {
            var centre = new GeoPoint(51.5074, -0.1278);
            var hash = Geohash.Encode(centre, 7);
            var cell = Geohash.DecodeToBoundingBox(hash);

            // Every cell in the 3x3 block should touch the expanded centre cell.
            var expanded = cell.Expand(cell.LatitudeSpan * 111, GeoCore.Units.DistanceUnit.Kilometers);

            Assert.That(Geohash.Neighbors(hash).Select(Geohash.Decode),
                Is.All.Matches<GeoPoint>(p => expanded.Contains(p)));
        }
    }
}
