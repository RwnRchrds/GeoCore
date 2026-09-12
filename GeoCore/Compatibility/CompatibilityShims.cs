#if NETSTANDARD2_0
// ---------------------------------------------------------------------------
// Small polyfills so the library can target netstandard2.0 (.NET Framework
// 4.6.1+, Unity, Xamarin) while still being written in modern C#.
// ---------------------------------------------------------------------------

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Required by the compiler to emit <c>init</c>-only setters and records.
    /// Present in-box from .NET 5 onwards.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}

namespace GeoCore.Compatibility
{
    /// <summary>
    /// Replacements for BCL members that are unavailable on netstandard2.0.
    /// </summary>
    internal static class MathCompat
    {
        internal static double Clamp(double value, double min, double max) =>
            value < min ? min : value > max ? max : value;
    }
}
#else
namespace GeoCore.Compatibility
{
    /// <summary>
    /// Replacements for BCL members that are unavailable on netstandard2.0.
    /// On modern targets these forward straight to the BCL.
    /// </summary>
    internal static class MathCompat
    {
        internal static double Clamp(double value, double min, double max) =>
            Math.Clamp(value, min, max);
    }
}
#endif
