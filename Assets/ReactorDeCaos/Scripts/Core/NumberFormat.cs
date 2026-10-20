using System;
using System.Globalization;

namespace ReactorDeCaos
{
    public static class NumberFormat
    {
        private static readonly string[] Suffix = { "", "K", "M", "B", "T", "Q" };
        public static string Compact(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) return "—";
            if (value >= 1e18) return value.ToString("0.00e+0", CultureInfo.InvariantCulture);
            int tier = 0;
            while (value >= 1000 && tier < Suffix.Length - 1) { value /= 1000; tier++; }
            return value.ToString(value >= 100 ? "0" : "0.#", CultureInfo.InvariantCulture) + Suffix[tier];
        }
    }
}
