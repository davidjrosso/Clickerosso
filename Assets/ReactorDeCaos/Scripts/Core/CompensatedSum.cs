using System;

namespace ReactorDeCaos
{
    internal static class CompensatedSum
    {
        // Keep the low-order part instead of dropping small frame income or costs.
        public static void Add(ref double value, ref double remainder, double delta)
        {
            double sum = value + delta;
            remainder += Math.Abs(value) >= Math.Abs(delta)
                ? (value - sum) + delta : (delta - sum) + value;
            double normalized = sum + remainder;
            remainder -= normalized - sum;
            value = normalized;
        }

        public static bool IsValid(double value, double remainder)
        {
            return !double.IsNaN(remainder) && !double.IsInfinity(remainder)
                && Math.Abs(remainder) <= value * 2.220446049250313e-16
                && (value < EconomyConfig.MaxEnergy || remainder <= 0);
        }
    }
}
