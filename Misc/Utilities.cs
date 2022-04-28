namespace Slaggy
{
    /// <summary>
    /// Utility class that provides helper functions
    /// </summary>
    public partial class Utilities
    {
        /// <summary>
        /// Gets the sign of '<paramref name="value"/>', but 0 instead returns 0.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>-1, 0, 1, depending on if <paramref name="value"/> is less, equal or more than 0.</returns>
        public static int Sign3(float value)
        {
            if (value < 0) return -1;
            if (value > 0) return 1;
            return 0;
        }

        /// <summary>
        /// Gets the sign of '<paramref name="value"/>' as a boolean, where >= 0 is True.
        /// </summary>
        public static bool Sign(float value) => value >= 0;

        /// <summary>
        /// Gets the sign of '<paramref name="value"/>', but 0 instead returns 1.
        /// </summary>
        public static int Sign2(float value)
        {
            if (value < 0) return -1;

            return 1;
        }
    }
}
