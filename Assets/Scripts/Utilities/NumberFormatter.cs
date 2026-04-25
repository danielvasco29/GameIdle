using System;

namespace GameIdle
{
    public static class NumberFormatter
    {
        private static readonly string[] Suffixes =
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No",
            "Dc", "UDc", "DDc", "TDc", "QaDc", "QiDc", "SxDc", "SpDc"
        };

        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "∞";
            if (value < 0) return "-" + Format(-value);
            if (value < 1000) return value.ToString("F1");

            int magnitude = Math.Min((int)(Math.Log10(value) / 3), Suffixes.Length - 1);
            double divided = value / Math.Pow(1000, magnitude);
            return divided.ToString("F2") + Suffixes[magnitude];
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.RoundToInt(seconds);
            if (s < 60) return $"{s}s";
            if (s < 3600) return $"{s / 60}m {s % 60:D2}s";
            return $"{s / 3600}h {(s % 3600) / 60:D2}m";
        }
    }
}
