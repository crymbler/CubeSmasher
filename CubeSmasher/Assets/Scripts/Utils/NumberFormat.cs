using System;
using System.Globalization;

public static class NumberFormat
{
    private const double ShortThreshold = 10000d;
    private static readonly string[] Suffixes = { "num.thousand", "num.million", "num.billion", "num.trillion" };

    public static string Full(double value)
    {
        return Math.Round(value).ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');
    }

    public static string Short(double value)
    {
        if (Math.Abs(value) < ShortThreshold) return Full(value);

        double scaled = value / 1000d;
        int step = 1;
        while (Math.Abs(scaled) >= 1000d && step < Suffixes.Length)
        {
            scaled /= 1000d;
            step++;
        }

        return Mantissa(scaled) + " " + UiText.Get(Suffixes[step - 1]);
    }

    private static string Mantissa(double value)
    {
        double abs = Math.Abs(value);
        string pattern = abs >= 100d ? "0" : abs >= 10d ? "0.0" : "0.00";
        return value.ToString(pattern, CultureInfo.InvariantCulture);
    }
}
