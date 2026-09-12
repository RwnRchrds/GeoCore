using System.Globalization;
using GeoCore.Core;

namespace GeoCore.Formats
{
    /// <summary>
    /// Parses coordinate pairs written in decimal degrees or in degrees/minutes/seconds.
    /// </summary>
    /// <remarks>
    /// <para>Recognised shapes include:</para>
    /// <list type="bullet">
    ///   <item><description><c>51.5074, -0.1278</c></description></item>
    ///   <item><description><c>51.5074 -0.1278</c></description></item>
    ///   <item><description><c>51.5074N 0.1278W</c></description></item>
    ///   <item><description><c>N51.5074 W0.1278</c></description></item>
    ///   <item><description><c>51°30'26.64"N, 0°07'40.08"W</c></description></item>
    ///   <item><description><c>51 30 26.64 N 0 7 40.08 W</c></description></item>
    ///   <item><description><c>0°07'40.08"W, 51°30'26.64"N</c> (hemisphere-led reversal)</description></item>
    /// </list>
    /// <para>
    /// Numbers are read with the invariant culture, so <c>.</c> is always the decimal
    /// separator and <c>,</c> always separates the two coordinates.
    /// </para>
    /// </remarks>
    public static class CoordinateParser
    {
        private enum TokenKind
        {
            Number,
            DegreeMark,
            MinuteMark,
            SecondMark,

            /// <summary>
            /// An 's' abutting a digit, which may mean "seconds" or "South". Resolved by
            /// <see cref="ResolveAmbiguousSeconds"/> once the surrounding tokens are known.
            /// </summary>
            SecondMarkOrSouth,

            Hemisphere,
            Separator
        }

        private readonly struct Token
        {
            internal Token(TokenKind kind, double number = 0, bool negated = false, char hemisphere = '\0')
            {
                Kind = kind;
                Number = number;
                Negated = negated;
                Hemisphere = hemisphere;
            }

            internal TokenKind Kind { get; }
            internal double Number { get; }
            internal bool Negated { get; }
            internal char Hemisphere { get; }
        }

        /// <summary>
        /// Parses a coordinate pair.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <returns>The parsed point.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The text could not be parsed as a coordinate pair.</exception>
        public static GeoPoint Parse(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (!TryParse(value, out var point, out var error))
                throw new FormatException(error);

            return point;
        }

        /// <summary>
        /// Attempts to parse a coordinate pair.
        /// </summary>
        /// <param name="value">The text to parse.</param>
        /// <param name="point">Receives the parsed point, or <c>default</c> on failure.</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
        public static bool TryParse(string? value, out GeoPoint point) =>
            TryParse(value, out point, out _);

        internal static bool TryParse(string? value, out GeoPoint point, out string error)
        {
            point = default;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                error = "Coordinate text was empty.";
                return false;
            }

            if (!TryTokenize(value!, out var tokens, out error))
                return false;

            ResolveAmbiguousSeconds(tokens);

            if (!TryFindBoundary(tokens, out var firstEnd, out var secondStart, out error))
                return false;

            if (!TryParseComponent(tokens, 0, firstEnd, out var first, out error) ||
                !TryParseComponent(tokens, secondStart, tokens.Count, out var second, out error))
            {
                return false;
            }

            // Hemisphere letters override position, so "0°W, 51°N" parses the same as
            // "51°N, 0°W". Without them, the conventional latitude-first order applies.
            var latitudeFirst = true;
            if (IsLongitudeHemisphere(first.Hemisphere) && IsLatitudeHemisphere(second.Hemisphere))
                latitudeFirst = false;
            else if (IsLongitudeHemisphere(first.Hemisphere) && second.Hemisphere == '\0')
                latitudeFirst = false;
            else if (first.Hemisphere == '\0' && IsLatitudeHemisphere(second.Hemisphere))
                latitudeFirst = false;

            var latitude = latitudeFirst ? first.Value : second.Value;
            var longitude = latitudeFirst ? second.Value : first.Value;

            if (!GeoPoint.IsValid(latitude, longitude))
            {
                error = FormattableString.Invariant(
                    $"Parsed coordinate ({latitude}, {longitude}) is out of range; latitude must be within ±90 and longitude within ±180.");
                return false;
            }

            point = new GeoPoint(latitude, longitude);
            return true;
        }

        /// <summary>
        /// Decides whether each digit-hugging 's' meant seconds or South.
        /// </summary>
        /// <remarks>
        /// A hemisphere letter immediately afterwards means the 's' was a unit marker, as in
        /// "0d7m40.08s W". Anything else — a number, a separator, the end of the string —
        /// means it was the hemisphere itself, as in "33.9249S, 18.4241W".
        /// </remarks>
        private static void ResolveAmbiguousSeconds(List<Token> tokens)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Kind != TokenKind.SecondMarkOrSouth)
                    continue;

                var followedByHemisphere =
                    i + 1 < tokens.Count && tokens[i + 1].Kind == TokenKind.Hemisphere;

                tokens[i] = followedByHemisphere
                    ? new Token(TokenKind.SecondMark)
                    : new Token(TokenKind.Hemisphere, hemisphere: 'S');
            }
        }

        private static bool TryTokenize(string value, out List<Token> tokens, out string error)
        {
            tokens = new List<Token>();
            error = string.Empty;

            var i = 0;
            while (i < value.Length)
            {
                var c = value[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (c == ',' || c == ';' || c == '/' || c == '|')
                {
                    tokens.Add(new Token(TokenKind.Separator));
                    i++;
                    continue;
                }

                if (c == '+' || c == '-' || char.IsDigit(c) || c == '.')
                {
                    var start = i;
                    var negated = c == '-';

                    if (c == '+' || c == '-')
                        i++;

                    var digitStart = i;
                    while (i < value.Length && (char.IsDigit(value[i]) || value[i] == '.'))
                        i++;

                    if (i == digitStart)
                    {
                        error = $"Unexpected '{c}' at position {start}.";
                        return false;
                    }

                    var text = value.Substring(digitStart, i - digitStart);
                    if (!double.TryParse(text, NumberStyles.AllowDecimalPoint,
                            CultureInfo.InvariantCulture, out var number))
                    {
                        error = $"'{text}' is not a valid number.";
                        return false;
                    }

                    tokens.Add(new Token(TokenKind.Number, number, negated));
                    continue;
                }

                // Seconds are written with a double quote or a doubled apostrophe.
                if (c == '"' || c == '″')
                {
                    tokens.Add(new Token(TokenKind.SecondMark));
                    i++;
                    continue;
                }

                if ((c == '\'' || c == '′' || c == '´' || c == '`') &&
                    i + 1 < value.Length &&
                    (value[i + 1] == '\'' || value[i + 1] == '′' || value[i + 1] == '´' || value[i + 1] == '`'))
                {
                    tokens.Add(new Token(TokenKind.SecondMark));
                    i += 2;
                    continue;
                }

                if (c == '\'' || c == '′' || c == '´' || c == '`' || c == 'm' || c == 'M')
                {
                    tokens.Add(new Token(TokenKind.MinuteMark));
                    i++;
                    continue;
                }

                if (c == '°' || c == 'º' || c == '*' || c == 'd' || c == 'D')
                {
                    tokens.Add(new Token(TokenKind.DegreeMark));
                    i++;
                    continue;
                }

                var upper = char.ToUpperInvariant(c);
                if (upper == 'N' || upper == 'S' || upper == 'E' || upper == 'W')
                {
                    // "51d30m26.64s N" uses 's' for seconds, but "51.5S" uses it for South.
                    // Only an 's' written hard against a digit is ambiguous at all.
                    if (upper == 'S' && i > 0 && char.IsDigit(value[i - 1]))
                    {
                        tokens.Add(new Token(TokenKind.SecondMarkOrSouth, hemisphere: 'S'));
                        i++;
                        continue;
                    }

                    tokens.Add(new Token(TokenKind.Hemisphere, hemisphere: upper));
                    i++;
                    continue;
                }

                error = $"Unexpected character '{c}' at position {i}.";
                return false;
            }

            if (tokens.Count == 0)
            {
                error = "Coordinate text contained no values.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Works out where the first coordinate ends and the second begins.
        /// </summary>
        private static bool TryFindBoundary(
            List<Token> tokens, out int firstEnd, out int secondStart, out string error)
        {
            firstEnd = 0;
            secondStart = 0;
            error = string.Empty;

            // An explicit separator is unambiguous, so it always wins.
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Kind != TokenKind.Separator)
                    continue;

                firstEnd = i;
                secondStart = i + 1;

                if (firstEnd == 0 || secondStart >= tokens.Count)
                {
                    error = "Coordinate text must contain two values separated by a comma.";
                    return false;
                }

                return true;
            }

            var hemisphereIndices = new List<int>();
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Kind == TokenKind.Hemisphere)
                    hemisphereIndices.Add(i);
            }

            if (hemisphereIndices.Count == 2)
            {
                // Whether hemisphere letters lead or trail is decided by the first token:
                // "N51 W0" prefixes, "51N 0W" suffixes.
                if (tokens[0].Kind == TokenKind.Hemisphere)
                {
                    firstEnd = hemisphereIndices[1];
                    secondStart = hemisphereIndices[1];
                }
                else
                {
                    firstEnd = hemisphereIndices[0] + 1;
                    secondStart = firstEnd;
                }

                if (firstEnd <= 0 || secondStart >= tokens.Count)
                {
                    error = "Coordinate text must contain two values.";
                    return false;
                }

                return true;
            }

            if (hemisphereIndices.Count > 2)
            {
                error = "Coordinate text contained more than two hemisphere markers.";
                return false;
            }

            // No separator and no usable hemisphere letters: split the numbers down the middle.
            var numberIndices = new List<int>();
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Kind == TokenKind.Number)
                    numberIndices.Add(i);
            }

            if (numberIndices.Count < 2 || numberIndices.Count % 2 != 0)
            {
                error = numberIndices.Count < 2
                    ? "Coordinate text must contain two values."
                    : FormattableString.Invariant(
                        $"Could not split {numberIndices.Count} numbers into a latitude and a longitude; separate them with a comma.");
                return false;
            }

            var half = numberIndices.Count / 2;
            firstEnd = numberIndices[half];
            secondStart = firstEnd;

            // Any trailing marks belong to the first coordinate, not the second.
            while (firstEnd > 0 && tokens[firstEnd - 1].Kind != TokenKind.Number &&
                   tokens[firstEnd - 1].Kind != TokenKind.Hemisphere)
            {
                break;
            }

            return true;
        }

        private static bool TryParseComponent(
            List<Token> tokens, int start, int end, out (double Value, char Hemisphere) result, out string error)
        {
            result = default;
            error = string.Empty;

            char hemisphere = '\0';
            var negated = false;
            var numbers = new List<double>();
            var sawSecondMark = false;

            for (var i = start; i < end; i++)
            {
                var token = tokens[i];

                switch (token.Kind)
                {
                    case TokenKind.Hemisphere:
                        if (hemisphere != '\0')
                        {
                            error = "A coordinate may only carry one hemisphere marker.";
                            return false;
                        }

                        hemisphere = token.Hemisphere;
                        break;

                    case TokenKind.Number:
                        if (numbers.Count == 3)
                        {
                            error = "A coordinate may contain at most degrees, minutes and seconds.";
                            return false;
                        }

                        if (token.Negated)
                        {
                            if (numbers.Count > 0)
                            {
                                error = "Only the degrees component may carry a sign.";
                                return false;
                            }

                            negated = true;
                        }

                        numbers.Add(token.Number);
                        break;

                    case TokenKind.SecondMark:
                        sawSecondMark = true;
                        break;

                    case TokenKind.DegreeMark:
                    case TokenKind.MinuteMark:
                    case TokenKind.Separator:
                        break;
                }
            }

            if (numbers.Count == 0)
            {
                error = "A coordinate was missing its numeric value.";
                return false;
            }

            // Only the least significant component may be fractional: "51.5 30'" is nonsense.
            for (var i = 0; i < numbers.Count - 1; i++)
            {
                if (numbers[i] != Math.Floor(numbers[i]))
                {
                    error = FormattableString.Invariant(
                        $"Only the smallest component of a sexagesimal coordinate may have a fractional part, but found {numbers[i]}.");
                    return false;
                }
            }

            var value = numbers[0];

            if (numbers.Count > 1)
            {
                if (numbers[1] >= 60.0)
                {
                    error = FormattableString.Invariant($"Minutes must be less than 60, but found {numbers[1]}.");
                    return false;
                }

                value += numbers[1] / 60.0;
            }

            if (numbers.Count > 2)
            {
                if (numbers[2] >= 60.0)
                {
                    error = FormattableString.Invariant($"Seconds must be less than 60, but found {numbers[2]}.");
                    return false;
                }

                value += numbers[2] / 3600.0;
            }
            else if (sawSecondMark && numbers.Count == 1)
            {
                error = "A seconds marker was given without minutes.";
                return false;
            }

            if (hemisphere != '\0')
            {
                if (negated)
                {
                    error = "A coordinate may not have both a negative sign and a hemisphere marker.";
                    return false;
                }

                if (hemisphere == 'S' || hemisphere == 'W')
                    value = -value;
            }
            else if (negated)
            {
                value = -value;
            }

            result = (value, hemisphere);
            return true;
        }

        private static bool IsLatitudeHemisphere(char hemisphere) =>
            hemisphere == 'N' || hemisphere == 'S';

        private static bool IsLongitudeHemisphere(char hemisphere) =>
            hemisphere == 'E' || hemisphere == 'W';
    }
}
