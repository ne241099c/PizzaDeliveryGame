using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Instant kitchen training, repeated purchases, legacy reservations, and save idempotency.</summary>
public static class KitchenTrainingValidation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        using var sampleMap=new SampleMapValidationScope();
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        var crew=UnityEngine.Object.FindFirstObjectByType<KitchenCrew>();
        var list=UnityEngine.Object.FindFirstObjectByType<OrderBook>();
        var cook=UnityEngine.Object.FindFirstObjectByType<CookingManager>();
        var training=UnityEngine.Object.FindFirstObjectByType<TrainingManager>();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var funding=new SerializedObject(economy);float original=funding.FindProperty("initialCash").floatValue;
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Kitchen training: "+name);count++;}
        bool Near(float actual,float expected)=>Mathf.Abs(actual-expected)<.0001f;
        try
        {
            campaign.NewGame();clock.AutomaticTick=false;spawner.enabled=false;
            funding.FindProperty("initialCash").floatValue=10000;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();
            var mio=crew.People.FirstOrDefault();Check(mio!=null,"a kitchen staff member to train");
            float speed=mio.Speed,care=mio.Care;decimal cash=economy.Cash,fee=training.Cost(mio,KitchenSkill.Speed);
            Check(fee>0 && training.Rejection(mio,KitchenSkill.Speed)=="","purchase available in preparation");
            Check(training.Request(mio,KitchenSkill.Speed) && !mio.InTraining && economy.Cash==cash-fee && Near(mio.Speed,speed+training.AbilityGain),"purchase pays once and improves speed immediately without absence");
            Check(training.Cost(mio,KitchenSkill.Speed)>fee,"price rises with trained ability");
            Check(training.Request(mio,KitchenSkill.Care) && Near(mio.Care,care+training.AbilityGain),"second purchase improves care on the same day");
            cash=economy.Cash;
            Check(!training.Cancel(mio) && economy.Cash==cash,"purchased upgrade has no refund");
            Check(training.KitchenAssignments.All(a=>a.Completed) && !training.HasAssignment(mio),"all purchases are complete and never reserve staff");
            var snapshot=save.SnapshotJson();
            Check(save.TryParse(snapshot,out var data,out var reason) && data.kitchenTraining.Count==2,"multiple completed purchases save: "+reason);
            var legacy=snapshot.Replace("\"kitchenTraining\"","\"ignoredTraining\"");
            Check(save.TryParse(legacy,out var older,out reason) && older.kitchenTraining.Count==0,"older saves without kitchen training load: "+reason);
            campaign.RestorePreparation(data);mio=crew.People.First();
            Check(!mio.InTraining && Near(mio.Speed,speed+training.AbilityGain) && Near(mio.Care,care+training.AbilityGain),"load retains values without applying completed purchases twice");
            campaign.StartBusiness();
            var order=new Order("Goal1","Training day",clock.ElapsedSeconds,clock.BusinessDuration);list.AddOrder(order);
            clock.Advance(2);
            Check(cook.StaffTask(mio)!=null || order.Baking!=BakingState.Waiting,"trained staff cooks immediately after opening");
            Check(!training.Request(mio,KitchenSkill.Speed),"purchase entry remains preparation only");
            campaign.NewGame();mio=crew.People.First();
            data=save.Capture();speed=mio.Speed;fee=training.Cost(mio,KitchenSkill.Speed);
            data.kitchenTraining.Add(new KitchenTrainingAssignment{StaffId=mio.Id,Skill=KitchenSkill.Speed,Fee=fee.ToString(CultureInfo.InvariantCulture)});
            data.cash=(economy.Cash-fee).ToString(CultureInfo.InvariantCulture);data.preparationCosts=fee.ToString(CultureInfo.InvariantCulture);
            Check(save.TryParse(JsonUtility.ToJson(data),out data,out reason),"old paid kitchen reservation validates: "+reason);
            cash=CampaignSaveManager.Decimal(data.cash);campaign.RestorePreparation(data);mio=crew.People.First();
            Check(!mio.InTraining && Near(mio.Speed,speed+training.AbilityGain) && economy.Cash==cash,"old reservation fulfilled on load without charging again");
            Check(save.TryParse(save.SnapshotJson(),out data,out reason),"converted reservation saves: "+reason);
            campaign.RestorePreparation(data);mio=crew.People.First();
            Check(Near(mio.Speed,speed+training.AbilityGain),"converted reservation does not grow twice");
            mio.Care=.95f;Check(training.Request(mio,KitchenSkill.Care) && Near(mio.Care,1),"gain clamps to maximum");
            Check(!training.Request(mio,KitchenSkill.Care),"maxed ability cannot be purchased");
            Check(UnityEngine.Object.FindFirstObjectByType<RecruitmentManager>().Fire(mio),"trained staff may be dismissed");
            Check(save.TryParse(save.SnapshotJson(),out _,out reason),"dismissed kitchen staff history still saves: "+reason);
            campaign.NewGame();mio=crew.People.First();
            funding.FindProperty("initialCash").floatValue=0;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();speed=mio.Speed;
            Check(!training.Request(mio,KitchenSkill.Speed) && Near(mio.Speed,speed),"insufficient funds cannot improve ability");
        }
        finally
        {
            funding.FindProperty("initialCash").floatValue=original;funding.ApplyModifiedPropertiesWithoutUndo();
            campaign.NewGame();clock.AutomaticTick=false;
        }
        return $"PASS: {count} kitchen training checks.";
    }
}
