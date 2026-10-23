using System;
using System.Linq;
using UnityEngine;

/// <summary>Pure mission checks: does not start a scene, tick a clock or access save files.</summary>
public static class RewardsMissionValidation
{
    public static string Run()
    {
        int checks = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception("Reward missions: " + name); checks++; }
        var definitions = MissionDefinition.Defaults();
        var first = RewardMissions.Generate(1234, 1, 1, definitions, null);
        string Ids(System.Collections.Generic.IEnumerable<MissionState> states) => string.Join("|", states.Select(s => s.id));
        Check(definitions.Count == 9 && definitions.Select(d => d.kind).Distinct().Count() == 9, "nine template kinds");
        Check(first.Count == 3 && first.Select(s => s.id).Distinct().Count() == 3, "exactly three distinct missions");
        Check(Ids(first) == Ids(RewardMissions.Generate(1234, 1, 1, definitions, null)), "same seed and day reproduce missions");
        Check(Ids(first) == Ids(RewardMissions.Generate(1234, 1, 1, definitions.AsEnumerable().Reverse(), null)), "template ordering does not change selection");
        Check(first.All(s => !s.selected && !s.completed && !s.rewarded), "mission selection is optional");
        var next = RewardMissions.Generate(1234, 2, 1, definitions, first.Select(s => s.id));
        Check(!next.Any(s => first.Any(p => p.id == s.id)), "previous day excluded with sufficient alternatives");
        var few = RewardMissions.Generate(1, 2, 1, definitions.Take(3), first.Select(s => s.id));
        Check(few.Count == 3 && few.Select(s => s.id).Distinct().Count() == 3, "small pool fills with distinct repeats");
        var high = RewardMissions.Generate(1234, 1, 4, definitions, null);
        Check(Ids(first) == Ids(high), "grade does not alter daily identities");
        foreach (var mission in high)
        {
            var def = RewardMissions.Lookup(definitions, mission.id);
            Check(mission.target == def.baseTarget + 3 * def.targetPerGrade, "grade target: " + mission.id);
        }
        Check(definitions.All(d => CampaignSaveManager.Decimal(d.cashReward) >= 30 &&
            CampaignSaveManager.Decimal(d.cashReward) + 3 * CampaignSaveManager.Decimal(d.cashRewardPerGrade) <= 100), "default cash rewards remain 30 through 100");
        Check(RewardMissions.Lookup(definitions, "missing") == null, "missing template lookup");
        foreach (var def in definitions)
        {
            var state = new MissionState { id = def.id, target = def.baseTarget };
            var metrics = new RewardDayMetrics();
            Check(!RewardMissions.Evaluate(def, state, metrics), "empty day does not qualify: " + def.id);
            metrics.delivered = 3; metrics.threeStar = state.target; metrics.maxBatch = state.target;
            metrics.backstreets = state.target; metrics.routeSavings = state.target;
            metrics.revenue = state.target.ToString(System.Globalization.CultureInfo.InvariantCulture); metrics.staffMade = state.target;
            Check(RewardMissions.Evaluate(def, state, metrics), "target boundary: " + def.id);
            Check(!state.selected && !state.completed && !state.rewarded, "evaluation is nonmutating: " + def.id);
            metrics.failedPizzas = 1; metrics.late = 1; metrics.wasted = 1;
            metrics.threeStar--; metrics.maxBatch--; metrics.backstreets--; metrics.routeSavings--;
            metrics.revenue = (state.target - 1).ToString(System.Globalization.CultureInfo.InvariantCulture); metrics.staffMade--;
            Check(!RewardMissions.Evaluate(def, state, metrics), "one below or one error: " + def.id);
        }
        var noLate = definitions.First(d => d.kind == MissionKind.NoLate);
        Check(!RewardMissions.Evaluate(noLate, new MissionState { id = noLate.id }, new RewardDayMetrics { delivered = 3, cancelled = 1 }), "cancelled orders defeat no-late mission");
        Check(!RewardMissions.Evaluate(null, first[0], new RewardDayMetrics()), "null template safe");
        var roundtrip = JsonUtility.FromJson<MissionState>(JsonUtility.ToJson(new MissionState
            { id = "revenue", target = 500, cashReward = "50.5", points = 2, selected = true, completed = true, rewarded = true }));
        Check(roundtrip.selected && roundtrip.completed && roundtrip.rewarded && roundtrip.cashReward == "50.5", "mission save roundtrip keeps selection and paid marker");
        return $"PASS: {checks} reward mission checks.";
    }
}
