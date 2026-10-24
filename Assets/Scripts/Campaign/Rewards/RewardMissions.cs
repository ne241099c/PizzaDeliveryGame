using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Statistics for one business day. Monetary values use invariant decimal strings.</summary>
[Serializable]
public sealed class RewardDayMetrics
{
    public int day, delivered, late, cancelled, threeStar, failedPizzas, wasted, staffMade, maxBatch, backstreets;
    public int oneStar,twoStar,accidents,spawned,closingCancelled,score,twoStarOnTime,threeStarOnTime,playerMade;
    public float busySeconds;
    public string[] staffNames=Array.Empty<string>();
    public int[] staffCounts=Array.Empty<int>();
    public float routeSavings;
    public string revenue = "0";
    public string[] madePizzas = Array.Empty<string>();
    public string[] threeStarPizzas = Array.Empty<string>();
    public string[] visitedDistricts = Array.Empty<string>();
    public string[] shortcuts = Array.Empty<string>();
}

public enum MissionKind
{
    ThreeStars, NoFailures, NoLate, Batch, Backstreets, RouteSavings, Revenue, NoWaste, StaffProduction
}

/// <summary>Editable template. Grade one uses the base values; increments apply for each higher grade.
/// Every mission of the day is active and pays automatically at settlement when achieved (2026-10-24).</summary>
[Serializable]
public sealed class MissionDefinition
{
    public string id;
    public MissionKind kind;
    public string title;
    public int baseTarget;
    public int targetPerGrade;
    public string cashReward = "30";
    public string cashRewardPerGrade = "0";
    public int points = 1;
    public int pointsPerGrade;

    // No "no late / no cancellation" mission: too hard for a daily task (user decision, 2026-10-24).
    // Every mission pays 100 yen.
    public static List<MissionDefinition> Defaults() => new List<MissionDefinition>
    {
        Make("three-stars", MissionKind.ThreeStars, "★3を{0}枚作る", 2, 1, "100", "0", 2),
        Make("no-failures", MissionKind.NoFailures, "失敗なしで営業する", 0, 0, "100", "0", 2),
        Make("batch-three", MissionKind.Batch, "1便で{0}件まとめて配達する", 3, 0, "100", "0", 3),
        Make("backstreets", MissionKind.Backstreets, "裏道を{0}回通る", 2, 2, "100", "0", 2),
        Make("route-savings", MissionKind.RouteSavings, "おすすめ経路より{0}秒速く配達する", 2, 1, "100", "0", 2),
        Make("revenue", MissionKind.Revenue, "売上{0}円を達成する", 1500, 750, "100", "0", 2),
        Make("no-waste", MissionKind.NoWaste, "廃棄なしで営業する", 0, 0, "100", "0", 1),
        Make("staff-production", MissionKind.StaffProduction, "内勤に{0}枚作ってもらう", 3, 2, "100", "0", 2)
    };

    static MissionDefinition Make(string id, MissionKind kind, string title, int target, int increment,
        string cash, string cashIncrement, int points) => new MissionDefinition
        {
            id = id, kind = kind, title = title, baseTarget = target, targetPerGrade = increment,
            cashReward = cash, cashRewardPerGrade = cashIncrement, points = points
        };
}

[Serializable]
public sealed class MissionState
{
    public string id;
    public bool selected, completed, rewarded;
    public int target;
    public string cashReward;
    public int points;
}

/// <summary>Pure daily selection and evaluation. Selection/payment/save ownership stays with the campaign.</summary>
public static class RewardMissions
{
    public static List<MissionState> Generate(int seed, int day, int grade,
        IEnumerable<MissionDefinition> definitions, IEnumerable<string> previousIds)
    {
        if (definitions == null) throw new ArgumentNullException(nameof(definitions));
        // Sorting makes the same seed independent of Inspector/list ordering.
        var unique = definitions.Where(d => d != null && !string.IsNullOrWhiteSpace(d.id))
            .GroupBy(d => d.id, StringComparer.Ordinal).Select(g => g.First())
            .OrderBy(d => d.id, StringComparer.Ordinal).ToList();
        if (unique.Count < 3) throw new ArgumentException("At least three distinct mission templates are required.", nameof(definitions));
        var previous = new HashSet<string>(previousIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        var fresh = unique.Where(d => !previous.Contains(d.id)).ToList();
        // Use our own integer generator so framework Random implementation changes do not change a saved day.
        uint random = unchecked((uint)seed * 747796405u + (uint)day * 2891336453u + 277803737u);
        Shuffle(fresh, ref random);
        var repeats = unique.Where(d => previous.Contains(d.id)).ToList();
        Shuffle(repeats, ref random);
        fresh.AddRange(repeats);
        int level = Math.Max(0, grade - 1);
        return fresh.Take(3).Select(d => new MissionState
        {
            id = d.id,
            selected = true,
            target = Scale(d.baseTarget, d.targetPerGrade, level),
            cashReward = Math.Max(0m, CampaignSaveManager.Decimal(d.cashReward)
                + CampaignSaveManager.Decimal(d.cashRewardPerGrade) * level).ToString(CultureInfo.InvariantCulture),
            points = Scale(d.points, d.pointsPerGrade, level)
        }).ToList();
    }

    public static MissionDefinition Lookup(IEnumerable<MissionDefinition> definitions, string id) =>
        definitions == null ? null : definitions.FirstOrDefault(d => d != null && d.id == id);

    /// <summary>Does not mutate selection or pay rewards. Zero-error missions require a delivered order.</summary>
    public static bool Evaluate(MissionDefinition definition, MissionState state, RewardDayMetrics metrics)
    {
        if (definition == null || state == null || metrics == null || state.id != definition.id) return false;
        switch (definition.kind)
        {
            case MissionKind.ThreeStars: return metrics.threeStar >= state.target;
            case MissionKind.NoFailures: return metrics.delivered > 0 && metrics.failedPizzas == 0;
            case MissionKind.NoLate: return metrics.delivered > 0 && metrics.late == 0 && metrics.cancelled == 0;
            case MissionKind.Batch: return metrics.maxBatch >= state.target;
            case MissionKind.Backstreets: return metrics.backstreets >= state.target;
            case MissionKind.RouteSavings: return !float.IsNaN(metrics.routeSavings) && !float.IsInfinity(metrics.routeSavings)
                && metrics.routeSavings >= state.target;
            case MissionKind.Revenue: return CampaignSaveManager.Decimal(metrics.revenue) >= state.target;
            case MissionKind.NoWaste: return metrics.delivered > 0 && metrics.wasted == 0;
            case MissionKind.StaffProduction: return metrics.staffMade >= state.target;
            default: return false;
        }
    }

    public static string Title(MissionDefinition definition, MissionState state) =>
        definition == null || state == null ? "" : string.Format(CultureInfo.InvariantCulture, definition.title ?? "", state.target);

    static int Scale(int value, int increment, int level) =>
        (int)Math.Max(0L, Math.Min(int.MaxValue, (long)value + (long)increment * level));

    static void Shuffle<T>(List<T> values, ref uint random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            random = unchecked(random * 1664525u + 1013904223u);
            int j = (int)(random % (uint)(i + 1));
            var value = values[i]; values[i] = values[j]; values[j] = value;
        }
    }
}
