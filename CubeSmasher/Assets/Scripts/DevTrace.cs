using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// TEMPORARY debug tracer. Delete this file and all DevTrace.* calls before publishing.
public static class DevTrace
{
    private static readonly Dictionary<string, double> Totals = new Dictionary<string, double>();
    private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();
    private static string _path;
    private static int _stage;
    private static float _stageStarted = -1f;
    private static int _hits;
    private static int _crits;

    public static void Money(string source, double amount)
    {
        Accumulate(source, amount);
        Line($"money {source} +{amount:F1} t={Now():F1}s");
    }

    public static void Purchase(string id, int level, double price)
    {
        Accumulate("spent", price);
        Count("buy " + id);
        Line($"buy {id} level={level} price={price:F0} t={Now():F1}s");
    }

    public static void Stage(int stage)
    {
        float now = Now();
        float started = _stageStarted < 0f ? 0f : _stageStarted;
        Line($"stage {_stage} -> {stage} took={now - started:F1}s hits={_hits} crits={_crits} t={now:F1}s");
        _stage = stage;
        _stageStarted = now;
        _hits = 0;
        _crits = 0;
        Summary();
    }

    public static void Hit(bool isCrit)
    {
        _hits++;
        if (isCrit) _crits++;
    }

    public static void Event(string text)
    {
        Count(text);
        Line($"event {text} t={Now():F1}s");
    }

    public static void Summary()
    {
        double earned = 0;
        foreach (var pair in Totals)
            if (pair.Key != "spent") earned += pair.Value;

        var sb = new StringBuilder();
        sb.AppendLine($"== summary t={Now():F1}s earned={earned:F0} spent={Get("spent"):F0} net={earned - Get("spent"):F0}");
        foreach (var pair in Totals)
        {
            string share = pair.Key != "spent" && earned > 0 ? $" ({pair.Value * 100 / earned:F1}%)" : "";
            sb.AppendLine($"   {pair.Key}: {pair.Value:F0}{share}");
        }
        foreach (var pair in Counts)
            sb.AppendLine($"   count {pair.Key}: {pair.Value}");
        Write(sb.ToString());
    }

    private static float Now() => Time.realtimeSinceStartup;

    private static double Get(string key) => Totals.TryGetValue(key, out double v) ? v : 0;

    private static void Accumulate(string key, double value)
    {
        Totals.TryGetValue(key, out double current);
        Totals[key] = current + value;
    }

    private static void Count(string key)
    {
        Counts.TryGetValue(key, out int current);
        Counts[key] = current + 1;
    }

    private static void Line(string text) => Write(text + "\n");

    private static void Write(string text)
    {
        if (_path == null)
        {
            _path = Path.Combine(Application.persistentDataPath, "devtrace.log");
            File.WriteAllText(_path, "");
        }
        Debug.Log("[DevTrace] " + text.TrimEnd());
        File.AppendAllText(_path, text);
    }
}
