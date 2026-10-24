using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>R-7 comparison using the existing simulation and real operating ledger. No balance overrides.</summary>
public static class RewardsMeasurement
{
    [Serializable]
    public sealed class MeasurementPolicy
    {
        public string name;
        public int seed = 104, maximumDays = 20;
        public float promiseSeconds = 60;
        public bool buyFacilities;
        public string repaymentFraction = "1";
        public int lastPurchaseDay = 99, repayFromDay = 1;
        public bool advertise = true;
    }

    [Serializable]
    public sealed class DayResult
    {
        public string policy;
        public int seed, day, grade, delivered, late, cancelled, threeStars, maxBatch, backstreets;
        public string cashBeforeRepayment, cash, loan, netAssets, score, rank, revenue, profit;
        public string reserve, repaid, interest, missionReward, missionResults;
        public bool bankrupt, normalResultPending;
        public float reputation;
        public string operatingCsv;
    }

    [Serializable]
    sealed class Report
    {
        public string generatedUtc, status, settings, notes;
        public MeasurementPolicy[] policies;
        public List<DayResult> days;
    }

    static readonly MeasurementPolicy[] Policies =
    {
        new MeasurementPolicy { name = "early-repay", buyFacilities = false, repaymentFraction = "1" },
        new MeasurementPolicy { name = "invest-first", buyFacilities = true, repaymentFraction = ".25" },
        new MeasurementPolicy { name = "invest-then-repay", buyFacilities = true, advertise = false, lastPurchaseDay = 12, repayFromDay = 13, repaymentFraction = "1" }
    };
    static readonly List<DayResult> results = new();
    public static IReadOnlyList<DayResult> Results => results;
    public static bool Done { get; private set; }
    public static string Status { get; private set; } = "Not run";
    static IDisposable protection;
    static DaySimulationSession session;
    static GameObject settingsObject;
    static DaySimulationSettings settings;
    static CampaignSaveManager save;
    static CampaignManager campaign;
    static GameClock clock;
    static BusinessEconomy economy;
    static RewardProgression rewards;
    static GameMode previousMode;
    static int policyIndex;
    static string settingsJson;
    static Dictionary<string, byte[]> protectedFiles;
    public static string OutputDirectory = "Docs/Results/Rewards_R7";

    static T Find<T>() where T : UnityEngine.Object
    {
        var value = UnityEngine.Object.FindFirstObjectByType<T>();
        if (value == null) throw new InvalidOperationException("R-7 missing component: " + typeof(T).Name);
        return value;
    }

    public static string Begin()
    {
        if (!Application.isPlaying || protection != null) throw new InvalidOperationException("R-7 needs Play and no active measurement.");
        save = Find<CampaignSaveManager>();
        campaign = Find<CampaignManager>(); clock = Find<GameClock>(); economy = Find<BusinessEconomy>();
        rewards = RewardProgression.Get(campaign);
        protectedFiles = new Dictionary<string, byte[]>();
        foreach (string path in new[] { save.SavePath, save.RecordsPath, Path.Combine(Path.GetDirectoryName(save.SavePath), "profile.json") })
            protectedFiles[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
        CampaignSaveManager.SessionSavingLocked = true;
        protection = save.SuppressSaving();
        previousMode = GameScenes.NewGameMode;
        clock.AutomaticTick = false;
        Done = false; results.Clear(); policyIndex = 0;
        try
        {
            var original = Find<DaySimulationSettings>();
            settingsJson = JsonUtility.ToJson(original, true);
            settingsObject = new GameObject("R7 measurement settings") { hideFlags = HideFlags.HideAndDontSave };
            settings = settingsObject.AddComponent<DaySimulationSettings>();
            JsonUtility.FromJsonOverwrite(settingsJson, settings);
            StartPolicy();
            return Status;
        }
        catch (Exception error)
        {
            Fail(error);
            throw;
        }
    }

    static void StartPolicy()
    {
        var policy = Policies[policyIndex];
        var serialized = new SerializedObject(settings);
        serialized.FindProperty("randomSeed").intValue = policy.seed;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GameScenes.NewGameMode = GameMode.Normal;
        session = new DaySimulationSession(settings, batchOverride: true, hireOverride: false,
            adjustOverride: false, name: policy.name, daysOverride: policy.maximumDays,
            promisesOverride: new[] { policy.promiseSeconds }, trainOverride: false,
            facilityOverride: policy.buyFacilities, citySeedOverride: policy.seed);
        session.LastPurchaseDay = policy.lastPurchaseDay;
        if (!policy.advertise) session.PurchaseOrder = session.PurchaseOrder.Where(t => t != FacilityType.Advertising).ToArray();
        session.DayCompleted += Record;
        Status = "Running " + policy.name + " / day 1";
    }

    /// <summary>Bounded work so callers can report progress and keep the Editor responsive.</summary>
    public static string Pump(int steps = 500)
    {
        if (Done) return Status;
        if (protection == null) throw new InvalidOperationException("Call R-7 Begin first.");
        if (steps < 1) throw new ArgumentOutOfRangeException(nameof(steps));
        try
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Play stopped during R-7 measurement.");
            for (int step = 0; step < steps && !Done; step++)
            {
                // Every mission is active automatically (2026-10-24): nothing to select before opening.
                session.Step();
                Status = "Running " + Policies[policyIndex].name + " / " + session.Progress;
                if (!session.Finished) continue;
                session.DayCompleted -= Record; session.Dispose(); session = null;
                clock.AutomaticTick = false;
                policyIndex++;
                if (policyIndex < Policies.Length) StartPolicy();
                else
                {
                    Done = true;
                    Status = "COMPLETE: " + results.Count + " operating days; bankruptcy ends each policy early.";
                    Cleanup(); WriteReports();
                }
            }
        }
        catch (Exception error) { Fail(error); }
        return Status;
    }

    static void Record(SimulationDayRecord record)
    {
        var policy = Policies[policyIndex];
        decimal cashBefore = economy.Cash;
        decimal reserve = economy.FixedCost(campaign.DayNumber + 1) + economy.TeamWages;
        decimal available = Math.Max(0, cashBefore - reserve);
        decimal amount = record.Day < policy.repayFromDay ? 0 : BusinessEconomy.Money(Math.Min(rewards.Loan,
            available * CampaignSaveManager.Decimal(policy.repaymentFraction)));
        if (amount > 0 && !campaign.IsBankrupt && !rewards.Repay(amount))
            throw new InvalidOperationException("R-7 repayment rejected: " + policy.name);
        if (campaign.IsBankrupt) amount = 0;
        var day = rewards.State.last ?? new RewardDayMetrics();
        results.Add(new DayResult
        {
            policy = policy.name, seed = policy.seed, day = record.Day, grade = rewards.State.grade,
            delivered = record.Delivered, late = record.Late, cancelled = record.Cancelled,
            threeStars = day.threeStar, maxBatch = day.maxBatch, backstreets = day.backstreets,
            cashBeforeRepayment = Money(cashBefore), cash = Money(economy.Cash), loan = Money(rewards.Loan),
            netAssets = Money(rewards.NetAssets), score = Money(rewards.Score),
            rank = campaign.IsBankrupt ? "" : rewards.RankFor(rewards.Score),
            revenue = Money(record.Revenue), profit = Money(record.Profit),
            reserve = Money(reserve), repaid = Money(amount), interest = rewards.State.interest,
            missionReward = rewards.State.missionCash,
            missionResults = string.Join(";", rewards.TodayMissions.Select(m =>
                m.id + ":selected=" + m.selected + ":completed=" + m.completed + ":rewarded=" + m.rewarded)),
            bankrupt = campaign.IsBankrupt, normalResultPending = rewards.HasPendingResult,
            reputation = record.Reputation, operatingCsv = record.Csv
        });
    }

    static string Money(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    static void Fail(Exception error)
    {
        Done = true; Status = "FAIL: " + error.Message;
        try { Cleanup(); }
        finally { WriteReports(); }
    }

    static void Cleanup()
    {
        try
        {
            if (session != null) { session.DayCompleted -= Record; session.Dispose(); session = null; }
            clock.AutomaticTick = false;
            CampaignSaveManager.SessionSavingLocked = true;
            GameScenes.NewGameMode = previousMode;
            if (settingsObject != null) UnityEngine.Object.DestroyImmediate(settingsObject);
            settingsObject = null; settings = null;
            foreach (var file in protectedFiles)
            {
                var after = File.Exists(file.Key) ? File.ReadAllBytes(file.Key) : null;
                if (!(file.Value == null ? after == null : after != null && file.Value.SequenceEqual(after)))
                    Status = "FAIL: protected campaign/records/profile changed during R-7.";
            }
        }
        finally { protection?.Dispose(); protection = null; }
    }

    static void WriteReports()
    {
        Directory.CreateDirectory(OutputDirectory);
        const string notes = "Both policies use the same town/order seed 104, 60-second promise, normal starting funds and abilities, 20-day cap. " +
            "No cash injection or game-balance edits. Batch dispatch enabled (existing simulation chooses at most two orders), courier hiring and training disabled. " +
            "Kitchen hiring, initial kitchen staff, optional automatic substitute, clock step and hiring/facility reserves inherit existing DaySimulationSettings below. " +
            "All three daily missions selected. Repayments reserve next day's fixed costs plus current team wages. Early-repay uses 100% of remaining cash; " +
            "invest-first enables existing facility purchases before opening and repays 25% of remaining cash. " +
            "Profit is the unchanged operating ledger profit; interest and mission money are separate. No player route optimization or manual cooking is simulated. " +
            "Premature bankruptcy is recorded, not bypassed; a full 20-day success cannot be inferred from a shortened run.";
        var report = new Report { generatedUtc = DateTime.UtcNow.ToString("o"), status = Status,
            settings = settingsJson, notes = notes, policies = Policies, days = results };
        File.WriteAllText(Path.Combine(OutputDirectory, "Measurement.json"), JsonUtility.ToJson(report, true));
        var csv = new StringBuilder("policy,seed,day,grade,cash_before_repayment,cash,loan,net_assets,score,rank,revenue,operating_profit,reserve,repaid,interest,mission_reward,reputation,delivered,late,cancelled,three_stars,max_batch,backstreets,bankrupt,result_pending,missions\n");
        foreach (var r in results) csv.AppendLine(string.Join(",", new object[] { r.policy,r.seed,r.day,r.grade,r.cashBeforeRepayment,r.cash,r.loan,r.netAssets,r.score,r.rank,r.revenue,r.profit,r.reserve,r.repaid,r.interest,r.missionReward,r.reputation,r.delivered,r.late,r.cancelled,r.threeStars,r.maxBatch,r.backstreets,r.bankrupt,r.normalResultPending,r.missionResults }.Select(Cell)));
        File.WriteAllText(Path.Combine(OutputDirectory, "Measurement.csv"), csv.ToString());
        var summary = new StringBuilder("# R-7 測定結果\n\n" + Status + "\n\n" + notes + "\n\n");
        foreach (var policy in Policies)
        {
            var final = results.LastOrDefault(r => r.policy == policy.name);
            summary.AppendLine(final == null ? policy.name + ": 未記録\n" :
                $"{policy.name}: {final.day}日目 / 破産={final.bankrupt} / 所持金={final.cash} / ローン={final.loan} / 純資産={final.netAssets} / グレード={final.grade} / スコア={final.score} / ランク={final.rank}\n");
        }
        summary.AppendLine("数値調整は行っていません。条件・初期ローン・昇格閾値・ランク閾値の採否はユーザー判断待ちです。");
        File.WriteAllText(Path.Combine(OutputDirectory, "README.md"), summary.ToString());
    }

    static string Cell(object value)
    {
        string text = value is IFormattable formatted ? formatted.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? "";
        return text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? text : "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
