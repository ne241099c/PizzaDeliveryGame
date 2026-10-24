using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Music and sound effects. Clips are Assets/Resources/Audio/&lt;name&gt; (CC0 sources: Docs/Credits_Audio.md), so a sound
/// is swapped by replacing its file. One object lives across scenes; on every scene load it listens to that scene's game
/// events (orders, the player's cooking, departures, deliveries, the day) and picks the music for the current screen.</summary>
public sealed class GameAudio : MonoBehaviour
{
    public static float MusicVolume=.35f,EffectVolume=.75f;
    private static GameAudio instance;
    private readonly Dictionary<string,AudioClip> clips=new();
    private readonly Dictionary<string,float> lastPlayed=new();
    private AudioSource music,effects,fanfare;
    // The opening fanfare is cut short: it fades over the last part of FanfareSeconds.
    private const float FanfareSeconds=3f,FanfareFade=.8f;
    private float fanfareStartedAt=-1;
    private string musicName="";
    private float musicHoldUntil;
    // Bound scene objects.
    private GameClock clock;private ScreenRouter router;private CookingManager cooking;private OrderBook orders;
    private DeliveryFlowController flow;private DayStats stats;
    private int orderCount,lateCount,cancelledCount;private readonly HashSet<PizzaJob> rang=new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if(instance!=null)return;
        var go=new GameObject("GameAudio");DontDestroyOnLoad(go);instance=go.AddComponent<GameAudio>();
    }
    private void Awake()
    {
        instance=this;
        music=gameObject.AddComponent<AudioSource>();music.loop=true;music.playOnAwake=false;
        effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;
        fanfare=gameObject.AddComponent<AudioSource>();fanfare.playOnAwake=false;
        SceneManager.sceneLoaded+=(_,_)=>Bind();
        Bind();
    }

    /// <summary>A one-shot effect; the same effect is not repeated within 60 ms.</summary>
    public static void Play(string name,float volume=1)
    {
        if(instance==null || !Application.isPlaying)return;
        float now=Time.unscaledTime;
        if(instance.lastPlayed.TryGetValue(name,out float at) && now-at<.06f)return;
        var clip=instance.Clip(name);if(clip==null)return;
        instance.lastPlayed[name]=now;instance.effects.PlayOneShot(clip,EffectVolume*volume);
    }
    /// <summary>Loops the named music (no restart when it is already playing).</summary>
    public static void Music(string name)
    {
        if(instance==null || instance.musicName==name)return;
        instance.musicName=name;var clip=instance.Clip(name);
        instance.music.Stop();if(clip==null)return;
        instance.music.clip=clip;instance.music.volume=MusicVolume;
        // While the opening fanfare plays the music waits, then comes in (Update).
        if(Time.unscaledTime<instance.musicHoldUntil)return;
        instance.music.Play();
    }
    /// <summary>The story music while the opening/ending plays, then the screen's music again.</summary>
    public static void StoryMusic(bool on)
    {
        if(instance==null)return;
        if(on)Music("bgm_story");
        else{instance.musicName="";instance.ScreenMusic();}
    }
    private AudioClip Clip(string name)
    {
        if(!clips.TryGetValue(name,out var clip)){clip=Resources.Load<AudioClip>("Audio/"+name);clips[name]=clip;}
        return clip;
    }

    private void Bind()
    {
        Unbind();
        // One listener for the whole game (the title scene has none); the sounds are 2D, so where it sits does not matter.
        foreach(var other in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))if(other.gameObject!=gameObject)other.enabled=false;
        if(GetComponent<AudioListener>()==null)gameObject.AddComponent<AudioListener>();
        clock=FindFirstObjectByType<GameClock>();router=FindFirstObjectByType<ScreenRouter>();cooking=FindFirstObjectByType<CookingManager>();
        orders=FindFirstObjectByType<OrderBook>();flow=FindFirstObjectByType<DeliveryFlowController>();stats=FindFirstObjectByType<DayStats>();
        if(clock!=null){clock.OnDayStarted+=DayStarted;clock.OnDayEnded+=DayEnded;}
        if(router!=null)router.Changed+=ScreenChanged;
        if(cooking!=null){cooking.PlayerActed+=PlayerActed;cooking.PizzaPacked+=Packed;}
        if(orders!=null){orders.OrdersChanged+=OrdersChanged;orderCount=orders.Orders.Count;}
        if(flow!=null){flow.Dispatched+=Dispatched;flow.RevenueEarned+=Delivered;}
        lateCount=stats!=null?stats.LateCount:0;cancelledCount=stats!=null?stats.CancelledCount:0;
        ScreenMusic();
    }
    private void Unbind()
    {
        if(clock!=null){clock.OnDayStarted-=DayStarted;clock.OnDayEnded-=DayEnded;}
        if(router!=null)router.Changed-=ScreenChanged;
        if(cooking!=null){cooking.PlayerActed-=PlayerActed;cooking.PizzaPacked-=Packed;}
        if(orders!=null)orders.OrdersChanged-=OrdersChanged;
        if(flow!=null){flow.Dispatched-=Dispatched;flow.RevenueEarned-=Delivered;}
        rang.Clear();
    }
    private void ScreenMusic()
    {
        if(Story.Current!=null){Music("bgm_story");return;}
        if(router==null){Music("bgm_prep");return;}
        Music(router.Current==ScreenId.Business?"bgm_business":router.Current==ScreenId.GameOver?"":"bgm_prep");
    }
    private void ScreenChanged(ScreenId id)=>ScreenMusic();
    private void DayStarted()
    {
        // The shop opens with a fanfare; the business music starts once it has rung out.
        var clip=Clip("day_start");music.Stop();
        musicHoldUntil=Time.unscaledTime+(clip!=null?Mathf.Min(clip.length,FanfareSeconds):0);
        if(clip==null)return;
        fanfare.clip=clip;fanfare.volume=EffectVolume;fanfare.Play();fanfareStartedAt=Time.unscaledTime;
    }
    private void DayEnded()=>Play("day_end");
    private void PlayerActed(string sound)=>Play(sound);
    private void Packed(PizzaJob job){if(job.Worker==PizzaWorker.Player)Play("boxed",.8f);}
    private void Dispatched(DeliveryTrip trip)=>Play("dispatch");
    private void Delivered(int amount)=>Play("delivered",.8f);
    private void OrdersChanged()
    {
        int n=orders.Orders.Count;
        if(n>orderCount && clock!=null && clock.Phase==DayPhase.Open)Play("order_new",.7f);
        orderCount=n;
    }
    private void Update()
    {
        if(fanfareStartedAt>=0)
        {
            float t=Time.unscaledTime-fanfareStartedAt;
            fanfare.volume=EffectVolume*Mathf.Clamp01((FanfareSeconds-t)/FanfareFade);
            if(t>=FanfareSeconds){fanfare.Stop();fanfareStartedAt=-1;}
        }
        if(music.clip!=null && !music.isPlaying && Time.unscaledTime>=musicHoldUntil && musicHoldUntil>0){musicHoldUntil=0;music.Play();}
        if(cooking==null || clock==null || clock.Phase!=DayPhase.Open)return;
        // The player's pizza reaches the green band: a ding, once per pizza.
        for(int i=0;i<cooking.OvenCount;i++)
        {
            var job=cooking.OvenJob(i);
            if(job==null || job.Worker!=PizzaWorker.Player || rang.Contains(job))continue;
            if(cooking.OvenElapsed(i)>=cooking.BandStartFor(job.Order)){rang.Add(job);Play("bake_ready");}
        }
        if(rang.Count>40)rang.RemoveWhere(j=>j.Stage==PizzaStage.Packed);
        if(stats!=null)
        {
            if(stats.LateCount>lateCount || stats.CancelledCount>cancelledCount)Play("late",.7f);
            lateCount=stats.LateCount;cancelledCount=stats.CancelledCount;
        }
    }
}
