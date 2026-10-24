using System;
using System.Linq;
using UnityEngine;

/// <summary>Plays the three tutorial chapters to the end through the same actions the player uses (cooking API, lanes,
/// the 出発 button, discarding), checking each step advances and nothing is saved. The frames are checked on screen
/// (Tools/Review/capture_tutorial.py), since the shop parts are laid out only in rendered frames.</summary>
public static class TutorialValidation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var cooking=UnityEngine.Object.FindFirstObjectByType<CookingManager>();
        var orders=UnityEngine.Object.FindFirstObjectByType<OrderBook>();
        var roster=UnityEngine.Object.FindFirstObjectByType<CourierRoster>();
        var handover=UnityEngine.Object.FindFirstObjectByType<HandoverManager>();
        var business=UnityEngine.Object.FindFirstObjectByType<BusinessView>();
        float duration=clock.BusinessDuration;
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Tutorial: "+name);count++;}
        TutorialDirector Start(int chapter)
        {
            campaign.NewGame();clock.AutomaticTick=false;
            var d=new GameObject("Tutorial (validation)").AddComponent<TutorialDirector>();d.Chapter=chapter;d.Begin();d.Poll();
            Check(d.StepIndex==0 && d.StepCount>=6 && d.Coach!=null && d.Coach.gameObject.activeInHierarchy,"chapter "+chapter+" opens with the coach card");
            Check(clock.Phase==DayPhase.Open && clock.BusinessDuration>=3600,"chapter "+chapter+" opens the shop for long enough");
            return d;
        }
        void Advance(TutorialDirector d,float seconds,Func<bool> until)
        {
            for(float t=0;t<seconds && !until();t+=.25f){clock.Advance(.25f);d.Poll();}
            d.Poll();d.Poll(); // a step moves one at a time: let the tutorial see the last change
        }
        TutorialDirector director=null;
        try
        {
            // ---- 1. 作る ----
            director=Start(1);
            var o=orders.Orders.Single();
            director.Next();director.Poll();
            Check(director.StepIndex==1,"drag the ticket step");
            Check(cooking.Claim(o,0),"claim on bench 0");director.Poll();
            Check(director.StepIndex==2 && cooking.AddDough(0),"dough step");director.Poll();
            Check(director.StepIndex==3 && cooking.AddSauce(0,1),"sauce step");director.Poll();
            Check(director.StepIndex==4 && cooking.AddCheese(0),"cheese step");director.Poll();
            Check(director.StepIndex==5 && cooking.AddTopping(0,o.RequiredTopping),"topping step");director.Poll();
            Check(director.StepIndex==6 && cooking.PutInOven(0,0),"oven step");director.Poll();
            Check(director.StepIndex==7,"take-out step");
            var job=cooking.JobFor(o);Advance(director,60,()=>job.Oven>=0 && cooking.OvenElapsed(job.Oven)>=(cooking.BandStartFor(o)+cooking.BandEndFor(o))/2);
            Check(cooking.TakeOut(job.Oven),"take out in the band");director.Poll();
            if(job.NeedsDecision)cooking.ServeAsIs(job); // a failed bake is served as it is (the step text covers both)
            Advance(director,30,()=>o.IsReady);
            Check(o.IsReady && director.StepIndex==9,$"boxed on the shelf, closing explanation (ready={o.IsReady} step={director.StepIndex} stage={job.Stage} decision={job.NeedsDecision} under={job.Undercooked} burnt={job.Burnt})");
            director.Next();director.Poll();Check(director.Finished,"chapter 1 finishes");
            Check(UnityEngine.Object.FindFirstObjectByType<KitchenCrew>().Count==0,"chapter 1 has no kitchen staff (the player cooks)");
            UnityEngine.Object.DestroyImmediate(director.gameObject);director=null;
            Check(clock.BusinessDuration==duration,"leaving the tutorial restores the business length");

            // ---- 2. 配達する ----
            director=Start(2);
            var ready=orders.Orders.Where(x=>x.IsReady).ToArray();Check(ready.Length==2,"two baked orders to deliver");
            director.Next();director.Poll();
            // Pin colours, the delivery promise, and lateness and cancellation.
            for(int i=1;i<=3;i++){Check(director.StepIndex==i,"delivery rules explanation "+i);director.Next();director.Poll();}
            var person=roster.People.OrderByDescending(p=>p.Capacity).First();
            Check(director.StepIndex==4,"drag to a lane step");
            Check(handover.Bag(ready[0],person),"first order into the lane");director.Poll();
            Check(director.StepIndex==5 && handover.Bag(ready[1],person),"second order into the same lane");director.Poll();
            Check(director.StepIndex==6,"route explanation");director.Next();director.Poll();
            var depart=business.GetComponentsInChildren<StickerButton>(true).FirstOrDefault(b=>b.name=="Depart_"+person.Name);
            Check(director.StepIndex==7,"the 出発 step");
            // The lanes are laid out in rendered frames; without one, call what the button calls.
            if(depart!=null)depart.Button.onClick.Invoke();else business.Depart(person);
            director.Poll();
            Check(person.CurrentTrip!=null && director.StepIndex==8,"departed, waiting step");
            Advance(director,600,()=>ready.All(x=>x.Status==OrderStatus.Delivered));
            Check(director.StepIndex==9,"both delivered, closing explanation");
            director.Next();director.Poll();Check(director.Finished,"chapter 2 finishes");
            UnityEngine.Object.DestroyImmediate(director.gameObject);director=null;

            // ---- 3. 内勤と作り直し ----
            director=Start(3);
            var first=orders.Orders.Single();
            director.Next();director.Poll();
            Advance(director,120,()=>first.IsReady);
            Check(first.IsReady && director.StepIndex==2,"the kitchen staff makes the first pizza");
            director.Next();director.Poll();
            Check(director.StepIndex==3,"the failed pizza is being baked");
            Advance(director,60,()=>director.StepIndex>3);
            var failed=cooking.Jobs.FirstOrDefault(j=>j.NeedsDecision && j.Worker==PizzaWorker.Player);
            Check(director.StepIndex==4 && failed!=null && failed.Worker==PizzaWorker.Player,"a failed pizza waits for the player's decision");
            Check(cooking.Discard(failed),"discard into the trash");director.Poll();
            Check(director.StepIndex==5,"remake step");
            Advance(director,180,()=>director.StepIndex>5);
            Check(director.StepIndex==6 && failed.Order.IsReady,"the kitchen staff takes the remake over and finishes it");
            director.Next();director.Poll();Check(director.Finished,"chapter 3 finishes");
        }
        finally
        {
            if(director!=null)UnityEngine.Object.DestroyImmediate(director.gameObject);
            campaign.NewGame();clock.AutomaticTick=false;
        }
        Check(clock.BusinessDuration==duration,"business length restored");
        return $"PASS: {count} tutorial checks.";
    }
}
