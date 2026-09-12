namespace GeoCore.Geodesy
{
    /// <summary>
    /// Thrown when an iterative geodesic calculation fails to converge.
    /// </summary>
    /// <remarks>
    /// In practice this only happens for very nearly antipodal points, where Vincenty's
    /// inverse formula is known not to converge. Use the spherical
    /// <see cref="Spherical.DistanceMeters"/> for those cases, or
    /// <see cref="Vincenty.TryInverse"/> to detect the condition without an exception.
    /// </remarks>
    [Serializable]
    public class GeodesyConvergenceException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeodesyConvergenceException"/> class.
        /// </summary>
        public GeodesyConvergenceException()
            : base("The geodesic calculation did not converge.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeodesyConvergenceException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public GeodesyConvergenceException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeodesyConvergenceException"/> class
        /// with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused this one.</param>
        public GeodesyConvergenceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if !NET8_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance of the <see cref="GeodesyConvergenceException"/> class
        /// from serialized data.
        /// </summary>
        /// <param name="info">The serialization store.</param>
        /// <param name="context">The streaming context.</param>
        protected GeodesyConvergenceException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
