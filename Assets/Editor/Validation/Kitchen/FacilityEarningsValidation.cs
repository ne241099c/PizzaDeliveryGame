using System;
using UnityEngine;

/// <summary>Equipment earns directly (2026-10-24): oven raises the pizza price, bike adds an on-time tip, workbench
/// lowers material cost; no shop-grade lock. Runs with the game's current amounts.</summary>
public static class FacilityEarningsValidation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var facilities=UnityEngine.Object.FindFirstObjectByType<FacilityManager>();
        var score=UnityEngine.Object.FindFirstObjectByType<ScoreManager>();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Facility earnings: "+name);count++;}
        try
        {
            campaign.NewGame();clock.AutomaticTick=false;
            var order=new Order("Goal1","Price",0,100);
            int price=score.BasePoints(order);decimal materials=economy.MaterialCost(10);
            int onTime=score.ScoreArrival(order,10,0);
            Check(facilities.OvenSalesBonus>0 && facilities.BikeTipBonus>0 && facilities.WorkbenchMaterialSaving>0,"direct earnings configured");
            economy.ApplyGoalCash(100000);
            Check(campaign.Rewards.State.grade==1 && facilities.Rejection(FacilityType.Workbench)=="" && facilities.Rejection(FacilityType.Oven)=="" && facilities.Rejection(FacilityType.Bike)=="","equipment is not locked by the first grade");
            Check(facilities.Purchase(FacilityType.Oven) && score.BasePoints(order)==Mathf.RoundToInt(price*(1+facilities.OvenSalesBonus)),"an oven raises the pizza price");
            Check(score.PriceOf(PizzaKind.Margherita)==score.BasePoints(order),"menu price shows the raised price");
            int raised=score.BasePoints(order);
            Check(facilities.Purchase(FacilityType.Bike),"bike purchase");
            int tip=Mathf.RoundToInt(raised*facilities.BikeTipBonus);
            Check(score.ScoreArrival(order,10,0)==onTime-price+raised+tip,"a bike adds an on-time tip of the pizza price");
            Check(score.TipForArrival(order,10,0)==tip && score.TipForArrival(order,200,0)==0,"tip shows on time only");
            Check(facilities.Purchase(FacilityType.Workbench) && economy.MaterialCost(10)==BusinessEconomy.Money(materials*(decimal)(1-facilities.WorkbenchMaterialSaving)),"a workbench lowers material cost");
            for(int i=0;i<10;i++)facilities.Purchase(FacilityType.Workbench);
            Check(facilities.MaterialShare>0,"materials never become free");
            campaign.NewGame();
            Check(score.BasePoints(order)==price && economy.MaterialCost(10)==materials,"a new game returns to the base amounts");
        }
        finally{campaign.NewGame();clock.AutomaticTick=false;}
        return $"PASS: {count} facility earnings checks.";
    }
}
