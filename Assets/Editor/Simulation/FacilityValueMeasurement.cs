using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>What each piece of equipment earns: the same town and orders for a few days with fixed equipment levels
/// (set directly, so the grade lock and purchase prices do not decide the result), compared against no equipment.
/// Plenty of starting cash keeps bankruptcy out of the comparison. Saving stays locked; protected files are compared.</summary>
public static class FacilityValueMeasurement
{
    public sealed class Variant
    {
        public string name; public FacilitySaveData state = new(); public int advertising;
    }
    public sealed class Total
    {
        public string variant; public int days, delivered, late, cancelled, spawned; public decimal revenue, profit, maintenance;
    }
    public static int Days = 5, KitchenStaff = -1; // -1: the settings decide
    public static int[] Seeds = { 104 };
    public static string OutputDirectory = "Docs/Results/Facility_Value";
    public static Variant[] Variants =
    {
        new Variant{name="none"},
        new Variant{name="oven+1",state=new FacilitySaveData{oven=1}},
        new Variant{name="oven+3",state=new FacilitySaveData{oven=3}},
        new Variant{name="bike+1",state=new FacilitySaveData{bike=1}},
        new Variant{name="bike+3",state=new FacilitySaveData{bike=3}},
        new Variant{name="workbench+1",state=new FacilitySaveData{workbench=1}},
        new Variant{name="workbench+3",state=new FacilitySaveData{workbench=3}},
        new Variant{name="menu+1",state=new FacilitySaveData{menu=1}},
        new Variant{name="menu+2",state=new FacilitySaveData{menu=2}},
        new Variant{name="ad1",advertising=1},
        new Variant{name="ad3",advertising=3},
        new Variant{name="all+3",state=new FacilitySaveData{oven=3,bike=3,workbench=3,menu=2}},
    };
    public static readonly List<Total> Totals = new();
    public static bool Done { get; private set; }
    public static string Status { get; private set; } = "Not run";
    static int index;
    static DaySimulationSession session;
    static GameObject settingsObject;
    static DaySimulationSettings settings;
    static FacilityManager facilities;
    static GameClock clock;
    static Dictionary<string, byte[]> protectedFiles;
    static IDisposable protection;

    static T Find<T>() where T : UnityEngine.Object
    {
        var value = UnityEngine.Object.FindFirstObjectByType<T>();
        if (value == null) throw new InvalidOperationException("Facility value: missing " + typeof(T).Name);
        return value;
    }

    public static string Begin()
    {
        if (!Application.isPlaying || protection != null) throw new InvalidOperationException("Needs Play and no active measurement.");
        var save = Find<CampaignSaveManager>();
        protectedFiles = new Dictionary<string, byte[]>();
        foreach (string path in new[] { save.SavePath, save.RecordsPath, Path.Combine(Path.GetDirectoryName(save.SavePath), "profile.json") })
            protectedFiles[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
        CampaignSaveManager.SessionSavingLocked = true;
        protection = save.SuppressSaving();
        facilities = Find<FacilityManager>(); clock = Find<GameClock>(); clock.AutomaticTick = false;
        settingsObject = new GameObject("Facility value settings") { hideFlags = HideFlags.HideAndDontSave };
        settings = settingsObject.AddComponent<DaySimulationSettings>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(Find<DaySimulationSettings>()), settings);
        Totals.Clear(); index = 0; Done = false;
        StartVariant();
        return Status;
    }

    static Variant Current => Variants[index / Seeds.Length];
    static void StartVariant()
    {
        var v = Current; int seed = Seeds[index % Seeds.Length];
        var serialized = new UnityEditor.SerializedObject(settings);
        serialized.FindProperty("randomSeed").intValue = seed; serialized.ApplyModifiedPropertiesWithoutUndo();
        session = new DaySimulationSession(settings, batchOverride: true, hireOverride: false, adjustOverride: false, name: v.name,
            daysOverride: Days, promisesOverride: new[] { 60f }, trainOverride: false, initialCashOverride: 100000,
            facilityOverride: false, citySeedOverride: seed, kitchenStaffOverride: KitchenStaff >= 0 ? KitchenStaff : null);
        facilities.Restore(v.state);
        if (Totals.Count == 0 || Totals[^1].variant != v.name) Totals.Add(new Total { variant = v.name });
        var t = Totals[^1];
        session.DayCompleted += r =>
        { t.days++; t.revenue += r.Revenue; t.profit += r.Profit; t.maintenance += r.EquipmentMaintenance;
            t.delivered += r.Delivered; t.late += r.Late; t.cancelled += r.Cancelled; t.spawned += r.Spawned;
        };
        Status = "Running " + v.name;
    }

    public static string Pump(int steps = 4000)
    {
        if (Done) return Status;
        try
        {
            for (int i = 0; i < steps && !Done; i++)
            {
                var v = Current;
                if (v.advertising > 0 && clock.Phase == DayPhase.BeforeOpening && facilities.Advertisement == 0)
                    facilities.Purchase(FacilityType.Advertising, v.advertising);
                session.Step();
                if (!session.Finished) continue;
                session.Dispose(); session = null; index++;
                if (index < Variants.Length * Seeds.Length) StartVariant(); else { Done = true; Status = "COMPLETE"; Cleanup(); Write(); }
            }
            if (!Done) Status = "Running " + Current.name + " seed " + Seeds[index % Seeds.Length] + " / " + session.Progress;
        }
        catch (Exception error) { Done = true; Status = "FAIL: " + error.Message; Cleanup(); }
        return Status;
    }

    static void Cleanup()
    {
        try
        {
            session?.Dispose(); session = null;
            if (settingsObject != null) UnityEngine.Object.DestroyImmediate(settingsObject);
            Find<CampaignManager>().NewGame(); clock.AutomaticTick = false;
            foreach (var file in protectedFiles)
            {
                var after = File.Exists(file.Key) ? File.ReadAllBytes(file.Key) : null;
                if (!(file.Value == null ? after == null : after != null && file.Value.SequenceEqual(after)))
                    Status = "FAIL: protected save files changed.";
            }
        }
        finally { protection?.Dispose(); protection = null; }
    }

    public static string Table()
    {
        var baseLine = Totals.FirstOrDefault(t => t.variant == "none");
        var text = new StringBuilder("variant,days,revenue,profit,maintenance,profit_vs_none_per_day,delivered,late,cancelled,spawned\n");
        foreach (var t in Totals)
        {
            decimal gain = baseLine == null || t.days == 0 ? 0 : (t.profit - baseLine.profit) / t.days;
            text.AppendLine(string.Join(",", t.variant, t.days, F(t.revenue), F(t.profit), F(t.maintenance), F(gain), t.delivered, t.late, t.cancelled, t.spawned));
        }
        return text.ToString();
    }
    static string F(decimal v) => decimal.Round(v, 1).ToString(CultureInfo.InvariantCulture);

    static void Write()
    {
        Directory.CreateDirectory(OutputDirectory);
        File.WriteAllText(Path.Combine(OutputDirectory, KitchenStaff >= 0 ? $"Measurement_kitchen{KitchenStaff}.csv" : "Measurement.csv"), Table());
    }
}
