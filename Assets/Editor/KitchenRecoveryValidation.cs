using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class KitchenRecoveryValidation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required");
        using var mapScope=new SampleMapValidationScope();
        T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
        var save=Find<CampaignSaveManager>();using var protection=save.SuppressSaving();
        var campaign=Find<CampaignManager>();var clock=Find<GameClock>();var spawner=Find<OrderSpawner>();var crew=Find<KitchenCrew>();var cook=Find<CookingManager>();var list=Find<OrderListUI>();var economy=Find<BusinessEconomy>();var hand=Find<ShopHand>();
        int? originalSeed=cook.SeedOverride;var fields=new SerializedObject(cook);float successBase=fields.FindProperty("staffSuccessBase").floatValue,successScale=fields.FindProperty("staffSuccessScale").floatValue;
        int count=0;void Check(bool ok,string text){if(!ok)throw new Exception("Kitchen recovery: "+text);count++;}
        void Start(bool staffed=false){cook.SeedOverride=1;campaign.NewGame();clock.AutomaticTick=false;spawner.enabled=false;if(!staffed)crew.Restore(Array.Empty<KitchenStaff>());campaign.StartBusiness();}
        Order Ticket(string title="Recovery",float limit=150){var o=new Order("Goal1",title,clock.ElapsedSeconds,limit);list.AddOrder(o);return o;}
        PizzaJob Assemble(Order o,float sauce=1,Topping topping=Topping.Basil){int b=Enumerable.Range(0,cook.BenchCount).First(i=>cook.ClaimRejection(o,i).Length==0);Check(cook.Claim(o,b) && cook.AddDough(b) && cook.AddSauce(b,sauce) && cook.AddCheese(b) && cook.AddTopping(b,topping),"assemble by production methods");return cook.JobFor(o);}
        void Quality(float b,float k){fields.Update();fields.FindProperty("staffSuccessBase").floatValue=b;fields.FindProperty("staffSuccessScale").floatValue=k;fields.ApplyModifiedPropertiesWithoutUndo();}
        try
        {
            Start();Check(cook.TwoStarBonus==15 && cook.ThreeStarBonus==40,"new quality bonuses");
            var o=Ticket();var job=Assemble(o,.2f,Topping.Pepperoni);cook.PutInOven(job.Bench,0);clock.Advance(cook.BakeDuration(o));cook.TakeOut(0);
            Check(job.NeedsDecision && !o.IsReady && !job.ToppingPoint && job.Stars==2,"wrong ingredients remain playable and await choice");
            clock.Advance(1);Check(!o.IsReady && job.Stage==PizzaStage.Baked,"not choosing leaves pizza on shelf");
            Check(cook.ServeAsIs(job) && o.IsReady && o.Stars==2 && job.ServeApproved,"serve accepts wrong pizza with no topping point");
            var oldDeadline=o.Deadline;decimal cash=economy.Cash;
            // Locate the registered product drop zone, independent of runtime UI parent names.
            var zone=UnityEngine.Object.FindObjectsByType<DropZone>(FindObjectsSortMode.None).First(z=>z.name=="Trash");
            var payload=new DragPayload{Kind=DragKind.Box,Order=o,Job=job,Sprite="box_closed"};
            Check(zone.Accepts(payload),"trash zone accepts packed pizza");
            hand.BeginAt(payload,Vector2.zero);hand.Cancel();Check(economy.Cash==cash && cook.JobFor(o)==job,"cancel drag costs nothing");
            list.SelectOnly(o);zone.Dropped(payload);
            Check(economy.Cash==cash-cook.WasteCost && economy.Ledger.Waste==cook.WasteCost && economy.Ledger.WastedPizzas==1,"drop charges exactly one waste fee");
            Check(!cook.Discard(job) && economy.Ledger.WastedPizzas==1,"stale item cannot be discarded twice");
            Check(cook.JobFor(o).Stage==PizzaStage.Dough && cook.IsRedo(o) && o.Deadline==oldDeadline && list.Contains(o) && !list.SelectedOrders.Contains(o),"redo starts dough on same ticket and deadline");
            Check(economy.Ledger.Profit==-cook.WasteCost,"waste belongs in profit without double charging");

            Start();o=Ticket();job=Assemble(o);cook.PutInOven(job.Bench,0);clock.Advance(cook.BandStartFor(o)/2);float baked=cook.OvenElapsed(0);cook.TakeOut(0);
            Check(job.Undercooked && !o.IsReady && !job.NeedsDecision && !cook.Box(job),"early removal is retained without packing");
            Check(cook.ResumeBaking(job,0) && Mathf.Abs(cook.OvenElapsed(0)-baked)<.001f,"return preserves elapsed baking time");
            clock.Advance(cook.BakeDuration(o)-baked);cook.TakeOut(0);
            Check(o.IsReady && o.Stars==3 && Mathf.Abs(cook.BusySeconds-cook.BakeDuration(o))<.001f,"resumed bake succeeds and oven usage is counted once");

            Start();o=Ticket();job=Assemble(o);cook.PutInOven(job.Bench,0);clock.Advance(cook.BurnAtFor(o)+.1f);cook.TakeOut(0);
            Check(job.Burnt && job.Stars==1 && job.NeedsDecision && !o.IsReady,"burnt pizza waits for the player's choice");
            Check(cook.ServeAsIs(job) && o.IsReady && o.Stars==1 && cook.BonusForArrival(o,clock.ElapsedSeconds)==0,"burnt pizza can be delivered with no quality bonus");
            var holder=Find<DeliveryPersonSelector>().People[0];handoverBag();
            void handoverBag(){Check(Find<HandoverManager>().Bag(o,holder),"burnt pizza can enter real delivery bag");}
            clock.Advance(Find<ScoreManager>().CancellationAt(o)-clock.ElapsedSeconds+.01f);
            Check(!list.Contains(o) && cook.JobFor(o)==null && !Find<HandoverManager>().IsBagged(o),"expired pizza and its bag entry are cleared");

            Start(true);var busy=Ticket("Existing task");var member=crew.People[0];var mine=Ticket("Abandoned partial");Check(cook.Claim(mine,1) && cook.AddDough(1) && cook.AddSauce(1,.2f),"claim and partially sauce second bench");
            job=cook.JobFor(mine);clock.Advance(cook.PlayerIdleSeconds+.01f);
            Check(cook.StaffTask(member)?.Order==busy && job.Worker==PizzaWorker.Player,"staff never interrupt current task for idle player pizza");
            clock.Advance(cook.StaffWorkSeconds(member)-clock.ElapsedSeconds+.01f);
            Check(job.Worker==PizzaWorker.Staff && cook.StaffTask(member)==job && job.SauceCoverage==.2f && !job.SaucePoint,"free staff inherit existing progress without improving mistakes");
            var queued=Ticket("Later order");clock.Advance(1);Check(cook.JobFor(queued)==null,"current inherited task takes priority over new tickets");

            Start(true);busy=Ticket("Hold cook for claim");mine=Ticket("Rescue oven");job=Assemble(mine);cook.PutInOven(job.Bench,0);list.RemoveOrders(new[]{busy});
            clock.Advance(cook.PlayerIdleSeconds-.01f);Check(job.Worker==PizzaWorker.Player && cook.OvenJob(0)==job,"no takeover before idle threshold");
            clock.Advance(.02f);Check(job.Worker==PizzaWorker.Staff && mine.IsReady && !job.Burnt,"free staff remove unattended pizza from oven");

            Start(true);busy=Ticket("Hold cook");mine=Ticket("Active player");Check(cook.Claim(mine,1) && cook.AddDough(1),"claim active dough");job=cook.JobFor(mine);list.RemoveOrders(new[]{busy});
            clock.Advance(cook.PlayerIdleSeconds-.5f);cook.Touch(job);clock.Advance(1);
            Check(job.Worker==PizzaWorker.Player,"fresh player interaction postpones assistance");
            clock.Advance(cook.PlayerIdleSeconds);Check(job.Worker==PizzaWorker.Staff,"abandoned work is eventually inherited");

            Quality(0,0);Start(true);o=Ticket("Staff failure");job=cook.JobFor(o);float required=cook.AutoRedoSeconds(job);
            Check(!float.IsInfinity(required) && required>=cook.StaffWorkSeconds(member)+cook.BakeDuration(o),"redo budget uses real work, bake and delivery time");
            var later=Ticket("Later normal");clock.Advance(cook.StaffWorkSeconds(crew.People[0])+cook.BakeDuration(o)+.01f);
            Check(cook.IsRedo(o) && economy.Ledger.WastedPizzas==1 && !o.IsReady,"zero-star-point staff pizza auto discards with time available");
            // The cook is completing the later task; redo must wait, then precede the next ordinary ticket.
            var newest=Ticket("Newest");clock.Advance(cook.StaffWorkSeconds(crew.People[0]));
            Check(cook.JobFor(o)!=null && cook.JobFor(o).Worker==PizzaWorker.Staff && cook.JobFor(o).StartedAt>=cook.StaffWorkSeconds(crew.People[0])+cook.BakeSeconds && cook.JobFor(newest)==null,"redo is first in waiting queue without interrupting an active task");
            Start(true);var shortOrder=Ticket("No time for redo",cook.StaffWorkSeconds(crew.People[0])+cook.BakeSeconds+.1f);
            clock.Advance(cook.StaffWorkSeconds(crew.People[0])+cook.BakeSeconds+.01f);
            Check(shortOrder.IsReady && shortOrder.Stars==1 && economy.Ledger.WastedPizzas==0,"staff serve failed pizza when redo cannot meet deadline");
            Quality(successBase,successScale);
            Start(true);busy=Ticket("Hold cook");mine=Ticket("Approved by player");job=Assemble(mine,0,Topping.Pepperoni);cook.PutInOven(job.Bench,0);clock.Advance(cook.BurnAt+.01f);cook.TakeOut(0);cook.ServeAsIs(job);list.RemoveOrders(new[]{busy});clock.Advance(cook.PlayerIdleSeconds+1);
            Check(mine.IsReady && mine.Stars==1 && job.ServeApproved && cook.RedoCount(mine)==0,"staff never remake player-approved pizza");
            Start();Check(economy.Ledger.Waste==0 && economy.Ledger.WastedPizzas==0 && !cook.Jobs.Any(),"new day resets recovery counters and jobs");
            return $"PASS: {count} kitchen recovery checks.";
        }
        finally{Quality(successBase,successScale);hand.Cancel();cook.SeedOverride=originalSeed;save.StartNewGame();clock.AutomaticTick=false;Find<TitleMenuUI>().ReturnTitle();}
    }
}
