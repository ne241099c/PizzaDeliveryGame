using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>The shop's money values (2026-10-24: loan 10000, every amount larger; 2026-10-25: loan 15000 and sales x2.8
/// after the user cleared in 8 of 20 days). Each value is written as
/// base x factor, so applying again changes nothing. The base values are the ones before the change; the older
/// validations are written against them and run inside <see cref="Legacy"/>.</summary>
public static class EconomyScale
{
    /// <summary>Costs (wages, materials, fixed costs, equipment, training, starting cash).</summary>
    public const float CostFactor=2.5f;
    /// <summary>Sales (pizza prices, quality bonus, late reduction). Larger than costs so the loan can be repaid.</summary>
    public const float RevenueFactor=2.8f;
    public const float InitialLoan=15000;
    /// <summary>Equipment maintenance on top of the cost factor (2026-10-24: halved so equipment pays back).</summary>
    public const float MaintenanceFactor=.5f;
    /// <summary>Direct earnings per upgrade: oven price +8%, bike on-time tip +8% of the price, workbench materials -20%
    /// (measured payback about 4-9 days, Docs/Results/Facility_Value).</summary>
    public const float OvenSalesBonus=.08f,BikeTipBonus=.08f,WorkbenchMaterialSaving=.2f;

    private enum Kind { Cost, Revenue, Maintenance }
    private static readonly (Type type,string field,float value,Kind kind)[] Values=
    {
        (typeof(BusinessEconomy),"accidentRepairCost",30,Kind.Cost),
        (typeof(BusinessEconomy),"kitchenWageBase",24,Kind.Cost),
        (typeof(BusinessEconomy),"kitchenWageAbilityFactor",30,Kind.Cost),
        (typeof(BusinessEconomy),"materialCostPerOrder",20,Kind.Cost),
        (typeof(BusinessEconomy),"fuelCostPerDistance",.4f,Kind.Cost),
        (typeof(BusinessEconomy),"wageBase",30,Kind.Cost),
        (typeof(BusinessEconomy),"wageAbilityFactor",24,Kind.Cost),
        (typeof(BusinessEconomy),"wageCapacityFactor",18,Kind.Cost),
        (typeof(BusinessEconomy),"dailyFixedCost",90,Kind.Cost),
        (typeof(BusinessEconomy),"fixedCostGrowthPerDay",15,Kind.Cost),
        (typeof(BusinessEconomy),"initialCash",300,Kind.Cost),
        (typeof(ScoreManager),"scorePerDelivery",100,Kind.Revenue),
        (typeof(ScoreManager),"pepperoniPoints",120,Kind.Revenue),
        (typeof(ScoreManager),"seafoodPoints",150,Kind.Revenue),
        (typeof(ScoreManager),"latePenaltyPerSecond",5,Kind.Revenue),
        (typeof(CookingManager),"qualityTwoStarBonus",15,Kind.Revenue),
        (typeof(CookingManager),"qualityThreeStarBonus",40,Kind.Revenue),
        (typeof(FacilityManager),"menuMaintenance",10,Kind.Maintenance),
        (typeof(FacilityManager),"ovenMaintenance",30,Kind.Maintenance),
        (typeof(FacilityManager),"bikeMaintenance",20,Kind.Maintenance),
        (typeof(FacilityManager),"expansionMaintenance",40,Kind.Maintenance),
        (typeof(FacilityManager),"workbenchMaintenance",15,Kind.Maintenance),
        (typeof(TrainingManager),"baseCost",80,Kind.Cost),
        (typeof(TrainingManager),"abilityCostFactor",150,Kind.Cost),
        (typeof(TrainingManager),"capacityCost",300,Kind.Cost),
    };
    private static readonly (Type type,string field,float[] values,Kind kind)[] Arrays=
    {
        (typeof(FacilityManager),"ovenPrices",new float[]{600,1000},Kind.Cost),
        (typeof(FacilityManager),"bikePrices",new float[]{500,900},Kind.Cost),
        (typeof(FacilityManager),"expansionPrices",new float[]{800,1400},Kind.Cost),
        (typeof(FacilityManager),"workbenchPrices",new float[]{400,700},Kind.Cost),
        (typeof(FacilityManager),"menuPrices",new float[]{400,700},Kind.Cost),
        (typeof(FacilityManager),"advertisingPrices",new float[]{100,200,350},Kind.Cost),
    };
    private static float Factor(Kind kind,float cost,float revenue,float maintenance)=>kind==Kind.Cost?cost:kind==Kind.Revenue?revenue:cost*maintenance;

    /// <summary>Writes the scaled values into the open Game scene (edit mode) and resets the mission and trophy lists
    /// to the code defaults, then saves the scene.</summary>
    public static string Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play first.");
        var log=new List<string>();
        Set(CostFactor,RevenueFactor,MaintenanceFactor,log,true);
        var equipment=new SerializedObject(Find(typeof(FacilityManager)));
        equipment.FindProperty("ovenSalesBonus").floatValue=OvenSalesBonus;
        equipment.FindProperty("bikeTipBonus").floatValue=BikeTipBonus;
        equipment.FindProperty("workbenchMaterialSaving").floatValue=WorkbenchMaterialSaving;
        equipment.ApplyModifiedProperties();
        var rewards=Find(typeof(RewardProgression));
        var so=new SerializedObject(rewards);
        so.FindProperty("initialLoan").floatValue=InitialLoan;
        so.FindProperty("payoffBonus").floatValue=100*CostFactor;
        so.FindProperty("goalNetAssets").floatValue=5000*CostFactor;
        var ranks=so.FindProperty("rankThresholds");float[] rankBase={15000,10000,6000,2000};
        ranks.arraySize=rankBase.Length;for(int i=0;i<rankBase.Length;i++)ranks.GetArrayElementAtIndex(i).floatValue=rankBase[i]*CostFactor;
        var grades=so.FindProperty("grades");float[] revenue={0,4000,12000,0},assets={0,0,0,4000};
        for(int i=0;i<Math.Min(grades.arraySize,revenue.Length);i++)
        {
            grades.GetArrayElementAtIndex(i).FindPropertyRelative("revenue").floatValue=revenue[i]*RevenueFactor;
            grades.GetArrayElementAtIndex(i).FindPropertyRelative("netAssets").floatValue=assets[i]*CostFactor;
        }
        so.ApplyModifiedProperties();
        // The scene kept its own copies of the lists; replace them with the new defaults (missions, trophies).
        var missions=MissionDefinition.Defaults();var trophies=RewardProfile.DefaultDefinitions();
        Undo.RecordObject(rewards,"Reset missions and trophies");
        typeof(RewardProgression).GetField("missionDefinitions",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(rewards,missions);
        typeof(RewardProgression).GetField("achievementDefinitions",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(rewards,trophies);
        EditorUtility.SetDirty(rewards);
        var scene=((Component)rewards).gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return $"Applied cost x{CostFactor}, revenue x{RevenueFactor}, loan {InitialLoan}; {missions.Count} missions, {trophies.Length} trophies. "+string.Join(", ",log);
    }

    /// <summary>Play mode, for validations written against the earlier amounts: puts the base values on the live
    /// components until disposed, including the earlier loan (4000) and payoff bonus (100), no mission pay, no direct equipment earnings and full maintenance. Grades and ranks are kept.</summary>
    public static IDisposable Legacy()
    {
        var previous=new List<(SerializedProperty property,float value)>();
        Set(1,1,1,null,false,previous);
        // The older checks predate the direct equipment earnings (oven price, bike tip, workbench materials).
        var equipment=new SerializedObject(Find(typeof(FacilityManager)));
        foreach(var field in new[]{"ovenSalesBonus","bikeTipBonus","workbenchMaterialSaving"})
        {
            var p=equipment.FindProperty(field);previous.Add((p.Copy(),Read(p)));Write(p,0);
        }
        equipment.ApplyModifiedPropertiesWithoutUndo();
        var rewards=new SerializedObject(Find(typeof(RewardProgression)));
        foreach(var (field,value) in new[]{("initialLoan",4000f),("payoffBonus",100f)})
        {
            var p=rewards.FindProperty(field);previous.Add((p.Copy(),Read(p)));Write(p,value);
        }
        // The older checks predate automatic mission pay: missions generated in the scope pay nothing.
        var texts=new List<(SerializedProperty property,string value)>();
        var missions=rewards.FindProperty("missionDefinitions");
        for(int i=0;i<missions.arraySize;i++)
        {
            var p=missions.GetArrayElementAtIndex(i).FindPropertyRelative("cashReward");texts.Add((p.Copy(),p.stringValue));p.stringValue="0";
        }
        rewards.ApplyModifiedPropertiesWithoutUndo();
        return new Restore(previous,texts);
    }
    private sealed class Restore:IDisposable
    {
        private List<(SerializedProperty property,float value)> previous;
        private List<(SerializedProperty property,string value)> texts;
        public Restore(List<(SerializedProperty,float)> values,List<(SerializedProperty,string)> strings){previous=values;texts=strings;}
        public void Dispose()
        {
            if(previous==null)return;
            foreach(var (property,value) in previous)
            {
                if(property.serializedObject.targetObject==null)continue;
                Write(property,value);property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var (property,value) in texts)
            {
                if(property.serializedObject.targetObject==null)continue;
                property.stringValue=value;property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            previous=null;texts=null;
        }
    }
    private static Object Find(Type type)
    {
        var found=Object.FindFirstObjectByType(type,FindObjectsInactive.Include);
        if(found==null)throw new InvalidOperationException("Open the Game scene first: missing "+type.Name);
        return found;
    }
    private static void Write(SerializedProperty property,float value)
    {
        if(property.propertyType==SerializedPropertyType.Integer)property.intValue=Mathf.RoundToInt(value);else property.floatValue=value;
    }
    private static float Read(SerializedProperty property)=>property.propertyType==SerializedPropertyType.Integer?property.intValue:property.floatValue;
    private static void Set(float cost,float revenue,float maintenance,List<string> log,bool withUndo,List<(SerializedProperty,float)> previous=null)
    {
        foreach(var group in Values.GroupBy(v=>v.type))
        {
            var so=new SerializedObject(Find(group.Key));
            foreach(var v in group)
            {
                var p=so.FindProperty(v.field);if(p==null)throw new InvalidOperationException("Missing field "+group.Key.Name+"."+v.field);
                previous?.Add((p.Copy(),Read(p)));
                Write(p,v.value*Factor(v.kind,cost,revenue,maintenance));log?.Add(v.field+"="+Read(p));
            }
            if(withUndo)so.ApplyModifiedProperties();else so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var group in Arrays.GroupBy(v=>v.type))
        {
            var so=new SerializedObject(Find(group.Key));
            foreach(var v in group)
            {
                var p=so.FindProperty(v.field);if(p==null)throw new InvalidOperationException("Missing field "+group.Key.Name+"."+v.field);
                p.arraySize=v.values.Length;
                for(int i=0;i<v.values.Length;i++)
                {
                    var element=p.GetArrayElementAtIndex(i);previous?.Add((element.Copy(),Read(element)));
                    Write(element,v.values[i]*Factor(v.kind,cost,revenue,maintenance));
                }
                log?.Add(v.field+"="+string.Join("/",Enumerable.Range(0,v.values.Length).Select(i=>Read(p.GetArrayElementAtIndex(i)))));
            }
            if(withUndo)so.ApplyModifiedProperties();else so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
