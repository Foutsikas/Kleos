using Godot;

public static class NumberFormatter
{
    // -------------------------------------------------------------------------
    // Suffix Table (short scale, up to decillion)
    // -------------------------------------------------------------------------

    private static readonly string[] Suffixes =
    {
        "",     // 10^0
        "K",    // 10^3  Thousand
        "M",    // 10^6  Million
        "B",    // 10^9  Billion
        "T",    // 10^12 Trillion
        "Qa",   // 10^15 Quadrillion
        "Qi",   // 10^18 Quintillion
        "Sx",   // 10^21 Sextillion
        "Sp",   // 10^24 Septillion
        "Oc",   // 10^27 Octillion
        "No",   // 10^30 Nonillion
        "Dc"    // 10^33 Decillion
    };
    // Beyond decillion (10^36+): switches to scientific notation
    // regardless of setting.

    // -------------------------------------------------------------------------
    // Main Format Method
    // -------------------------------------------------------------------------

    // Formats a number into compact display form.
    // Respects the scientific notation setting from SettingsManager.
    public static string FormatCompact(double value)
    {
        if (value < 0)
            return "-" + FormatCompact(-value);

        // Below 1000: always show full integer
        if (value < 1000.0)
            return ((long)value).ToString("N0");

        // Check if user prefers scientific notation
        bool useScientific = SettingsManager.Instance != null
            && SettingsManager.Instance.ScientificNotation;

        // Scientific notation only kicks in at 1 million and above.
        // Below that, use standard suffixes (1.0K, 12.3K, 123K, etc.)
        if (useScientific && value >= 1000000.0)
            return FormatScientific(value);

        return FormatWithSuffixes(value);
    }

    // Float overload for convenience
    public static string FormatCompact(float value)
    {
        return FormatCompact((double)value);
    }

    // -------------------------------------------------------------------------
    // Suffix Formatting (Standard Mode)
    // -------------------------------------------------------------------------

    private static string FormatWithSuffixes(double value)
    {
        int tier = 0;
        double reduced = value;

        while (reduced >= 1000.0 && tier < Suffixes.Length - 1)
        {
            reduced /= 1000.0;
            tier++;
        }

        if (reduced >= 1000.0)
            return FormatScientific(value);

        // Truncate to 2 decimal places (floor, not round)
        double truncated = System.Math.Floor(reduced * 100.0) / 100.0;

        // 100.0 - 999.9: no decimal ("123Dc")
        if (reduced >= 100.0)
            return $"{(int)reduced}{Suffixes[tier]}";

        // 1.0 - 99.9: two decimals, drop trailing zeros
        string formatted = $"{truncated:F2}".TrimEnd('0').TrimEnd('.');
        return $"{formatted}{Suffixes[tier]}";
    }

    // -------------------------------------------------------------------------
    // Scientific Notation
    // -------------------------------------------------------------------------

    private static string FormatScientific(double value)
    {
        if (value < 1.0)
            return "0";

        int exponent = (int)System.Math.Floor(System.Math.Log10(value));
        double mantissa = value / System.Math.Pow(10, exponent);

        // Truncate to 2 decimal places (floor, not round)
        mantissa = System.Math.Floor(mantissa * 100.0) / 100.0;

        // Clean display: 1.00e6 -> 1e6, 2.50e6 -> 2.5e6, 2.23e6 -> 2.23e6
        if (System.Math.Abs(mantissa - (int)mantissa) < 0.001)
            return $"{(int)mantissa}e{exponent}";

        // Drop trailing zero: 2.50 -> 2.5
        string formatted = $"{mantissa:F2}".TrimEnd('0').TrimEnd('.');
        return $"{formatted}e{exponent}";
    }

    // -------------------------------------------------------------------------
    // Full Precision (no abbreviation, with thousand separators)
    // -------------------------------------------------------------------------

    public static string FormatFull(double value)
    {
        return ((long)value).ToString("N0");
    }

    public static string FormatFull(float value)
    {
        return ((long)value).ToString("N0");
    }

    // -------------------------------------------------------------------------
    // Cost Display
    // -------------------------------------------------------------------------

    // Below 10K: full number with separators ("2,500")
    // Above 10K: compact form ("15.0K", "1.2M")
    public static string FormatCost(double value)
    {
        if (value < 10000.0)
            return ((long)value).ToString("N0");

        return FormatCompact(value);
    }

    public static string FormatCost(float value)
    {
        return FormatCost((double)value);
    }
}