using System;
using System.Linq;
using UnityEngine;

/// <summary>Kitchen staff training: booking and refund in preparation, the save, absence during the first half of
/// business, and the gain when training ends. Saving is suppressed; the normal save is never written.</summary>
public static class KitchenTrainingValidation
{
    public static string Run()
    {
        using var sampleMap=new SampleMapValidationScope();
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        var crew=UnityEngine.Object.FindFirstObjectByType<KitchenCrew>();
        var list=UnityEngine.Object.FindFirstObjectByType<OrderBook>();
        var cook=UnityEngine.Object.FindFirstObjectByType<CookingManager>();
        var training=UnityEngine.Object.FindFirstObjectByType<TrainingManager>();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Kitchen training: "+name);count++;}
        try
        {
            campaign.NewGame();clock.AutomaticTick=false;spawner.enabled=false;
            var mio=crew.People.FirstOrDefault();Check(mio!=null,"a kitchen staff member to train");
            float speed=mio.Speed,care=mio.Care;decimal cash=economy.Cash,fee=training.Cost(mio,KitchenSkill.Speed);
            Check(fee>0 && training.Rejection(mio,KitchenSkill.Speed)=="","training can be booked in preparation");
            Check(training.Request(mio,KitchenSkill.Care) && mio.InTraining && economy.Cash==cash-training.KitchenAssignments[0].Paid,"booking pays the fee and takes the staff member off the line");
            Check(training.Rejection(mio,KitchenSkill.Speed).Length>0 && !training.Request(mio,KitchenSkill.Speed),"one training per staff member per day");
            Check(training.Cancel(mio) && !mio.InTraining && economy.Cash==cash && training.KitchenAssignments.Count==0,"cancelling refunds in full");
            Check(training.Request(mio,KitchenSkill.Speed),"book again");
            var snapshot=save.SnapshotJson();
            Check(save.TryParse(snapshot,out var data,out var reason) && data.kitchenTraining.Count==1 && data.kitchenTraining[0].StaffId==mio.Id && data.kitchenTraining[0].Skill==KitchenSkill.Speed,"the booking is saved: "+reason);
            var legacy=snapshot.Replace("\"kitchenTraining\"","\"ignoredTraining\"");
            Check(save.TryParse(legacy,out var older,out reason) && older.kitchenTraining!=null && older.kitchenTraining.Count==0,"older saves without kitchen training still load: "+reason);
            campaign.RestorePreparation(data);
            mio=crew.People.First(s=>s.Id==data.kitchenTraining[0].StaffId);
            Check(mio.InTraining && training.KitchenAssignments.Count==1,"continue restores the booking");

            campaign.StartBusiness();
            var order=new Order("Goal1","Training day",clock.ElapsedSeconds,clock.BusinessDuration);list.AddOrder(order);
            clock.Advance(2);
            Check(cook.StaffTask(mio)==null && order.Baking==BakingState.Waiting,"staff in training does not cook");
            clock.Advance(training.EndsAt-clock.ElapsedSeconds+.01f);
            Check(!mio.InTraining && Mathf.Abs(mio.Speed-(speed+training.AbilityGain))<.0001f && Mathf.Abs(mio.Care-care)<.0001f,"training raises the chosen ability half way through business");
            clock.Advance(.5f);
            Check(cook.StaffTask(mio)!=null || order.Baking!=BakingState.Waiting,"after training the staff member cooks again");
        }
        finally{campaign.NewGame();}
        return $"PASS: {count} kitchen training checks.";
    }
}
