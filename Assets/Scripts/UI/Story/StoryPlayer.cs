using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>One picture of the opening or the ending: Build draws it on the stage and may add animators (seconds since the
/// scene began). No people are drawn: the city, the shop, pizzas and numbers tell the story.</summary>
public sealed class StoryScene
{
    public string Caption;
    public Action<RectTransform,List<Action<float>>> Build;
}

/// <summary>Plays story scenes full screen above everything: the caption types in, a click (Space/Enter) shows it all or goes
/// on, スキップ (Esc) ends at once. Scenes cross through a short fade; when the last one is done the overlay is removed and
/// Done runs.</summary>
public sealed class StoryPlayer : MonoBehaviour
{
    public IList<StoryScene> Scenes;
    public Action Done;
    public int Index=>index;
    public bool Finished{get;private set;}
    public int SceneCount=>Scenes?.Count??0;
    public RectTransform Stage=>stage;
    private RectTransform root,stage;
    private CanvasGroup fade;
    private TextMeshProUGUI caption;
    private readonly List<Action<float>> animators=new();
    private int index=-1;
    private float sceneAt,leavingAt=-1;
    private const float FadeSeconds=.35f,CharsPerSecond=26;
    private UITheme T=>UiKit.T;

    public static StoryPlayer Play(string name,IList<StoryScene> scenes,Action done)
    {
        var player=new GameObject(name).AddComponent<StoryPlayer>();
        player.Scenes=scenes;player.Done=done;player.Build();player.Show(0);
        GameAudio.StoryMusic(true);
        return player;
    }
    private void Build()
    {
        var go=new GameObject("StoryCanvas",typeof(RectTransform));go.layer=5;go.transform.SetParent(transform,false);
        var canvas=go.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=900;
        var scaler=go.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        go.AddComponent<GraphicRaycaster>();
        var holder=UiKit.Stretch(go.transform,"Holder");
        // Fixed 1920×1080 drawing area, letterboxed in the middle of any screen.
        root=UiKit.Box(holder,"Story",0,0,1920,1080);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
        UiKit.Fill(UiKit.Stretch(holder,"Back"),T.line,0).transform.SetAsFirstSibling();
        var hit=UiKit.Fill(UiKit.Stretch(root,"Click"),new Color(0,0,0,0),0,true);
        var click=hit.gameObject.AddComponent<Button>();click.targetGraphic=hit;click.transition=Selectable.Transition.None;click.onClick.AddListener(Advance);
        stage=UiKit.Box(root,"Stage",0,0,1920,1080);
        var band=UiKit.Box(root,"CaptionBand",0,880,1920,200);UiKit.Fill(band,new Color(.1f,.07f,.05f,.82f),0);
        caption=UiKit.Label(band,"Caption","",160,24,1600,120,40,Color.white,FontRole.Display,TextAlignmentOptions.Center,true);
        UiKit.Label(band,"Hint","クリックで次へ ▼",1460,148,420,40,T.captionSize,new Color(1,1,1,.7f),FontRole.Display,TextAlignmentOptions.MidlineRight);
        UiKit.Button(root,"StorySkip","スキップ",StickerKind.Secondary,1680,28,200,64,T.subheadingSize,Finish,FontRole.Display);
        var veil=UiKit.Box(root,"Fade",0,0,1920,1080);UiKit.Fill(veil,T.line,0);fade=UiKit.Group(veil);fade.blocksRaycasts=false;
    }
    private void Show(int i)
    {
        index=i;sceneAt=Time.unscaledTime;leavingAt=-1;animators.Clear();
        for(int c=stage.childCount-1;c>=0;c--)Destroy(stage.GetChild(c).gameObject);
        var scene=Scenes[i];scene.Build?.Invoke(stage,animators);
        caption.text=scene.Caption;caption.maxVisibleCharacters=0;
        Tick();
    }
    /// <summary>Shows the whole caption, or goes to the next scene (the last one finishes).</summary>
    public void Advance()
    {
        if(Finished || leavingAt>=0)return;
        if(caption.maxVisibleCharacters<caption.text.Length){sceneAt-=caption.text.Length/CharsPerSecond;return;}
        leavingAt=Time.unscaledTime;GameAudio.Play("page",.6f);
    }
    public void Finish()
    {
        if(Finished)return;Finished=true;
        Destroy(gameObject);Done?.Invoke();
        GameAudio.StoryMusic(false);
    }
    private void Update()
    {
        if(Finished)return;
        var k=Keyboard.current;
        if(k!=null){if(k.escapeKey.wasPressedThisFrame){Finish();return;}if(k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)Advance();}
        if(leavingAt>=0 && Time.unscaledTime-leavingAt>=FadeSeconds){if(index+1<Scenes.Count)Show(index+1);else{Finish();return;}}
        Tick();
    }
    private void Tick()
    {
        float t=Time.unscaledTime-sceneAt;
        foreach(var a in animators)a(t);
        caption.maxVisibleCharacters=Mathf.Clamp(Mathf.FloorToInt((t-.4f)*CharsPerSecond),0,caption.text.Length);
        fade.alpha=leavingAt>=0?Mathf.Clamp01((Time.unscaledTime-leavingAt)/FadeSeconds):1-Mathf.Clamp01(t/FadeSeconds);
    }
}
