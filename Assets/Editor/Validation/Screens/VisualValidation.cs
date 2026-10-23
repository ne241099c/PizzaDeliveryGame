using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
/// <summary>The look of the Game scene: theme fonts and sizes, sticker buttons, the map's layers, cameras and the
/// layout of what is shown. LayoutReport is shared by the other screen validations. Play Mode only.</summary>
public static class VisualValidation
{
    [MenuItem("Tools/Pizza Delivery/Validate Visuals (Play Mode)")]
    public static void Menu()=>Debug.Log(Run());
    public static string Run()
    {
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Visual: "+name);count++;}
        var theme=UITheme.Active;var fonts=new[]{theme.font,theme.displayFont,theme.boldFont,theme.blackFont};
        Check(fonts.All(f=>f!=null) && theme.captionSize>=16 && theme.bodySize==20,"theme fonts and the 16px minimum");
        foreach(FontRole role in Enum.GetValues(typeof(FontRole)))Check(fonts.Contains(UiKit.Font(role)),"font role uses a theme font "+role);
        Check(theme.roundSprite!=null && theme.circleSprite!=null,"shared shapes are sprite assets");
        Check(UiStrings.Japanese("Start business")=="営業開始" && UiStrings.Japanese("Delivery promise: 60s").StartsWith("お届け目安"),"central Japanese table");
        Check(UiStrings.Japanese("Aoi / Drive 0.6 / Goal1").Contains("Aoi / 運転 0.6 / Goal1"),"presentation preserves names IDs and numbers");

        var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();var root=(RectTransform)canvas.transform;
        Check(canvas.GetComponentsInChildren<TMP_Text>(true).All(t=>t.font!=null && fonts.Contains(t.font) && t.fontSize>=theme.captionSize),"every text uses a theme font at 16px or more");
        Check(canvas.GetComponentsInChildren<Button>(true).All(b=>(b.GetComponent<StickerButton>()!=null && b.GetComponent<StickerButton>().Label!=null)
            || (b.GetComponent<StickerCardAction>()!=null && b.targetGraphic!=null && b.transition==Selectable.Transition.ColorTint && b.colors.highlightedColor==theme.cheeseLight)),"every button is a sticker button or a sticker card");
        var scaler=canvas.GetComponent<CanvasScaler>();
        Check(scaler.uiScaleMode==CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.referenceResolution==new Vector2(1920,1080) && scaler.screenMatchMode==CanvasScaler.ScreenMatchMode.Expand,"one 1920x1080 layout that expands to other aspect ratios");

        // The town on the map: roads, addresses and decoration are separate layers.
        var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();var edges=UnityEngine.Object.FindObjectsByType<EdgeView>(FindObjectsSortMode.None);
        Check(edges.Length==map.Edges.Count && edges.All(e=>e.GetComponentInChildren<LineRenderer>()!=null),"edge views and click geometry");
        Check(UnityEngine.Object.FindObjectsByType<NodeView>(FindObjectsSortMode.None).Length==map.Nodes.Count,"node views");
        var decoration=GameObject.Find("CityDecoration");
        Check(UnityEngine.Object.FindFirstObjectByType<MapVisuals>()!=null && decoration!=null && decoration.transform.childCount>0,"independent city decoration layer");
        Check(theme.CourierColor("Aoi")==theme.CourierColor("Aoi") && theme.CourierColor("Aoi")!=theme.CourierColor("Ren"),"stable courier identity colors without random draws");
        var clone=UnityEngine.Object.Instantiate(edges[0]);
        try{var a=map.Nodes[0];var b=map.Nodes[1];var road=new MapEdge("ReviewOneWay",a.Id,b.Id,1,RoadType.Main,.5f){Bidirectional=false};clone.Initialize(road,a,b,map,false);Check(clone.transform.Find("OneWayArrow")!=null,"one-way road has a direction arrow");}
        finally{UnityEngine.Object.DestroyImmediate(clone.gameObject);}
        var main=Camera.main;var clear=UnityEngine.Object.FindFirstObjectByType<ScreenClearCamera>();var clearView=clear!=null?clear.GetComponent<Camera>():null;
        Check(clearView!=null && main!=null && clearView!=main && clearView.rect==new Rect(0,0,1,1) && clearView.depth<main.depth && clearView.cullingMask==0 && clearView.clearFlags==CameraClearFlags.SolidColor,"full-screen clear behind the map camera");

        // Feedback timing and colors; switching animations off changes nothing in the game.
        var feedback=UnityEngine.Object.FindFirstObjectByType<DeliveryFeedback>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        Check(theme.DeadlineColor(.8f)==theme.good && theme.DeadlineColor(.3f)==theme.money && theme.DeadlineColor(.1f)==theme.bad,"deadline green yellow red thresholds");
        Check(theme.effectSeconds>=.5f && theme.effectSeconds<=1.5f,"short feedback duration");
        decimal cash=economy.Cash;float elapsed=clock.ElapsedSeconds;bool enabled=feedback.AnimationsEnabled;feedback.AnimationsEnabled=false;
        Check(!DeliveryFeedback.Animate && economy.Cash==cash && clock.ElapsedSeconds==elapsed,"animation switch leaves cash and clock untouched");feedback.AnimationsEnabled=enabled;

        // The preparation screen as shown.
        var router=UnityEngine.Object.FindFirstObjectByType<ScreenRouter>();var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();
        using(save.SuppressSaving()){save.StartNewGame();router.Show(ScreenId.Hub,true);}
        var hub=UnityEngine.Object.FindFirstObjectByType<HubView>();hub.ShowTab(0);
        string report=LayoutReport();Check(report.Length==0,"preparation screen has no overflow or overlap: "+report);
        return $"PASS: {count} visual checks.";
    }
    /// <summary>Issues on the screen as currently shown: text larger than its box, overlapping buttons, and text outside the canvas.
    /// Visibility follows the CanvasGroup chain directly, so it is correct immediately after a screen change.</summary>
    public static string LayoutReport()
    {
        var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();var root=(RectTransform)canvas.transform;Canvas.ForceUpdateCanvases();
        var issues=new System.Collections.Generic.List<string>();var corners=new Vector3[4];
        Rect Bounds(RectTransform r){r.GetWorldCorners(corners);var a=root.InverseTransformPoint(corners[0]);var b=root.InverseTransformPoint(corners[2]);return Rect.MinMaxRect(a.x,a.y,b.x,b.y);}
        foreach(var text in canvas.GetComponentsInChildren<TextMeshProUGUI>(false))
        {
            if(!Shown(text.transform) || string.IsNullOrWhiteSpace(text.text))continue;
            text.ForceMeshUpdate();var rect=text.rectTransform.rect;var preferred=text.GetPreferredValues(text.text,rect.width,0);
            bool tall=text.overflowMode==TextOverflowModes.Overflow && preferred.y>rect.height+2;bool wide=text.textWrappingMode==TextWrappingModes.NoWrap && text.overflowMode==TextOverflowModes.Overflow && preferred.x>rect.width+2;
            if(tall || wide)issues.Add("text overflow "+Path(text.transform)+" "+rect.size+" needs "+preferred);
            if(text.GetComponentInParent<RectMask2D>()==null){var b=Bounds(text.rectTransform);if(b.xMin<root.rect.xMin-2 || b.xMax>root.rect.xMax+2 || b.yMin<root.rect.yMin-2 || b.yMax>root.rect.yMax+2)issues.Add("off screen "+Path(text.transform));}
        }
        var buttons=canvas.GetComponentsInChildren<Button>(false).Where(b=>Shown(b.transform)).ToArray();
        for(int i=0;i<buttons.Length;i++)for(int j=i+1;j<buttons.Length;j++)
        {
            var a=buttons[i];var b=buttons[j];
            if(a.transform.IsChildOf(b.transform) || b.transform.IsChildOf(a.transform))continue;
            // Scrolled lists are compared only with their own members.
            if(a.GetComponentInParent<ScrollRect>()!=b.GetComponentInParent<ScrollRect>())continue;
            var overlap=Bounds((RectTransform)a.transform);var other=Bounds((RectTransform)b.transform);
            float w=Mathf.Min(overlap.xMax,other.xMax)-Mathf.Max(overlap.xMin,other.xMin),h=Mathf.Min(overlap.yMax,other.yMax)-Mathf.Max(overlap.yMin,other.yMin);
            if(w>1 && h>1)issues.Add("buttons overlap "+Path(a.transform)+" / "+Path(b.transform));
        }
        return string.Join("; ",issues);
    }
    private static bool Shown(Transform t)
    {
        if(!t.gameObject.activeInHierarchy)return false;
        for(;t!=null;t=t.parent){var group=t.GetComponent<CanvasGroup>();if(group!=null){if(group.alpha<=0)return false;if(group.ignoreParentGroups)break;}}
        return true;
    }
    private static string Path(Transform t){string path=t.name;for(int i=0;i<2 && t.parent!=null;i++){t=t.parent;path=t.name+"/"+path;}return path;}
}
