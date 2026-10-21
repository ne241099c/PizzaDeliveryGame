using System;
using UnityEditor;
using UnityEngine;
public static partial class M6Validation
{
    public static int Checks { get; private set; }
    public static void Check(bool ok,string name) { if(!ok)throw new Exception("M6: "+name);Checks++; }
    public static void Near(float actual,float expected,string name) => Check(Mathf.Abs(actual-expected)<.001f,name+" actual "+actual+" expected "+expected);
    [MenuItem("Tools/Pizza Delivery/Validate M6 (Play Mode)")]
    public static void Menu() => Debug.Log(Run());
    private static void ValidatePotentials()
    {
        var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>().People;
        foreach(var p in roster)for(int i=0;i<5;i++)
        {var a=(CourierAbility)i;Near(p.Potential(a),Mathf.Min(1,p.Ability(a)+.25f),"initial potential bonus");Near(p.Experience(a),0,"initial experience");}
        var fixture=new DeliveryPerson("Clamp",.5f,.5f,2,.5f,.5f,.5f);
        fixture.SetPotential(CourierAbility.Driving,.6f);fixture.Driving=1;Near(fixture.Driving,.6f,"ability setter clamps to potential");
        fixture.SetPotential(CourierAbility.Capacity,1);fixture.Capacity=2;Check(fixture.Capacity==1,"capacity upper bound");
        fixture.SetExperience(CourierAbility.Service,123);var copy=fixture.Clone();Near(copy.Experience(CourierAbility.Service),123,"copy preserves experience");Near(copy.Potential(CourierAbility.Driving),.6f,"copy preserves potential");
        var views=UnityEngine.Object.FindObjectsByType<CourierAbilitiesView>(FindObjectsInactive.Include,FindObjectsSortMode.None);Check(views.Length>=3,"ability bars on business and preparation rows");
    }
    public static string Run()
    {
        Checks=0;string previous=M5Validation.Run()+" / "+M4PlayValidation.CheckReset();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        Check(economy.FixedCost(1)==150 && economy.FixedCost(2)==165 && economy.FixedCost(30)==585,"fixed cost growth");
        var settings=UnityEngine.Object.FindFirstObjectByType<DaySimulationSettings>();
        Near(settings.AdjustedPromise(60,0,90),45,"shorter promise with no late and high reputation");
        Near(settings.AdjustedPromise(60,3,50),75,"longer promise with late orders");
        Near(settings.AdjustedPromise(60,1,100),60,"other cases preserve promise");
        var fixture=new GameObject("Busy fixture");fixture.SetActive(false);
        try {var stats=fixture.AddComponent<DayStats>();Near(stats.BusyRatio(2,300),0,"zero busy ratio");}
        finally {UnityEngine.Object.DestroyImmediate(fixture);}
        ValidatePotentials();
        return previous+$" / PASS: {Checks} M6 checks.";
    }
}
