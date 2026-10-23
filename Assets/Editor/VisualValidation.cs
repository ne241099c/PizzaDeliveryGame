using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class VisualValidation
{
    [MenuItem("Tools/Pizza Delivery/Validate Visuals (Play Mode)")]
    public static void Menu()=>Debug.Log(Run());
    public static string Run()
    {
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Visual: "+name);count++;}
        var theme=UITheme.Active;Check(theme!=null && theme.font!=null && theme.roundedPanel!=null,"shared theme and licensed Japanese font");
        Check(theme.caption>=14 && theme.body==18 && theme.padding==16 && theme.cornerRadius==8,"readable type and spacing defaults");
        var go=new GameObject("ThemeValidation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
        try
        {
            var panel=go.AddComponent<ThemedPanel>();panel.Apply(theme);Check(go.GetComponent<Image>().color==theme.background,"panel reads theme");
            var label=new GameObject("Label",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));label.transform.SetParent(go.transform,false);var text=label.AddComponent<ThemedText>();text.Apply(theme);Check(label.GetComponent<TMP_Text>().font==theme.font && label.GetComponent<TMP_Text>().fontSize==theme.body,"text reads theme");
            var button=go.AddComponent<ThemedButton>();foreach(ThemeButtonKind kind in Enum.GetValues(typeof(ThemeButtonKind))){button.kind=kind;button.Apply(theme);Check(go.GetComponent<Button>().transition==Selectable.Transition.ColorTint,"button hover and role "+kind);}
            float original=theme.body;try{theme.body=22;text.Apply(theme);Check(label.GetComponent<TMP_Text>().fontSize==22,"theme edit updates component");}finally{theme.body=original;}
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
        var driver=UnityEngine.Object.FindFirstObjectByType<UIThemeDriver>();if(driver!=null)
        {
            Check(UiStrings.Japanese("Start business")=="営業開始" && UiStrings.Japanese("Delivery promise: 60s").StartsWith("お届け目安"),"central Japanese table");
            Check(UiStrings.Japanese("Aoi / Drive 0.6 / Goal1").Contains("Aoi / 運転 0.6 / Goal1"),"presentation preserves names IDs and numbers");
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();driver.Refresh();
            var shop=canvas.GetComponentInChildren<ShopUiRoot>(true);
            var styledSelf=canvas.GetComponentsInChildren<ThemeOptOut>(true);
            bool Redesigned(Component c)=>shop!=null && c.transform.IsChildOf(shop.transform) || styledSelf.Any(o=>c.transform.IsChildOf(o.transform));
            var shopFonts=new[]{theme.font,theme.displayFont,theme.boldFont,theme.blackFont};
            Check(canvas.GetComponentsInChildren<TMP_Text>(true).All(t=>Redesigned(t)?t.font!=null && shopFonts.Contains(t.font) && t.fontSize>=theme.captionSize:t.font==theme.font && t.fontSize>=theme.caption),"all screens use their readable licensed Japanese theme fonts");
            Check(canvas.GetComponentsInChildren<Button>(true).All(b=>Redesigned(b)?(b.GetComponent<StickerButton>()!=null && b.GetComponent<StickerButton>().Label!=null) || (b.GetComponent<StickerCardAction>()!=null && b.targetGraphic!=null && b.transition==Selectable.Transition.ColorTint && b.colors.highlightedColor==theme.cheeseLight) || !b.gameObject.activeSelf && b.GetComponent<ThemedButton>()!=null:b.GetComponent<ThemedButton>()!=null),"all button actions retain their themed component (hidden legacy menu buttons stay inside the modals)");
            if(shop!=null)
            {
                Check(shopFonts.All(f=>f!=null) && theme.captionSize>=16 && theme.bodySize==20,"redesign font family and minimum readable size");
                foreach(FontRole role in Enum.GetValues(typeof(FontRole)))Check(shopFonts.Contains(UiKit.Font(role)),"redesign role uses theme font "+role);
            }
            foreach(var view in canvas.GetComponentsInChildren<CourierAbilitiesView>(true).Where(v=>v==v.GetComponent<CourierAbilitiesView>()))
            {
                var labels=new SerializedObject(view).FindProperty("labels");
                for(int i=0;i<labels.arraySize;i++){var label=(TMP_Text)labels.GetArrayElementAtIndex(i).objectReferenceValue;var group=label.GetComponent<CanvasGroup>();Check(group==null || group.alpha==1,"actual ability label retained "+view.name+" / "+i);}
            }
        }
        if(driver!=null && driver.ImplementationStage>=2)
        {
            var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();var edges=UnityEngine.Object.FindObjectsByType<EdgeView>(FindObjectsSortMode.None);
            Check(edges.Length==map.Edges.Count && edges.All(e=>e.GetComponentInChildren<UnityEngine.LineRenderer>()!=null),"existing edge views and click geometry retained");
            // Generated towns (G-0..G-5) name their blocks District_N under the CityDecoration layer.
            Check(UnityEngine.Object.FindFirstObjectByType<MapVisuals>()!=null && GameObject.Find("CityDecoration")!=null && GameObject.Find("CityDecoration").transform.Cast<Transform>().Any(t=>t.name.StartsWith("District_")),"independent city decoration layer");
            Check(UITheme.Active.CourierColor("Aoi")==UITheme.Active.CourierColor("Aoi") && UITheme.Active.CourierColor("Aoi")!=UITheme.Active.CourierColor("Ren"),"stable courier identity colors without random draws");
            Check(UnityEngine.Object.FindObjectsByType<NodeView>(FindObjectsSortMode.None).Length==map.Nodes.Count,"existing node views reused");
            var clone=UnityEngine.Object.Instantiate(edges[0]);try{var a=map.Nodes[0];var b=map.Nodes[1];var road=new MapEdge("ReviewOneWay",a.Id,b.Id,1,RoadType.Main,.5f){Bidirectional=false};clone.Initialize(road,a,b,map,false);Check(clone.transform.Find("OneWayArrow")!=null,"one-way road has a direction arrow");}finally{UnityEngine.Object.DestroyImmediate(clone.gameObject);}
        }
        if(driver!=null && driver.ImplementationStage>=3)
        {
            var feedback=UnityEngine.Object.FindFirstObjectByType<DeliveryFeedback>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
            Check(theme.DeadlineColor(.8f)==theme.good && theme.DeadlineColor(.3f)==theme.money && theme.DeadlineColor(.1f)==theme.bad,"deadline green yellow red thresholds");
            Check(theme.effectSeconds>=.5f && theme.effectSeconds<=1.5f,"short feedback duration");
            var ledger=economy.Ledger;decimal cash=economy.Cash;float elapsed=clock.ElapsedSeconds;bool enabled=feedback.AnimationsEnabled;feedback.AnimationsEnabled=false;feedback.Render();feedback.AnimationsEnabled=enabled;
            Check(economy.Cash==cash && economy.Ledger==ledger && clock.ElapsedSeconds==elapsed,"feedback toggle leaves cash ledger and clock untouched");
            var timer=UIVisibility.Find(UnityEngine.Object.FindFirstObjectByType<Canvas>().transform,"TimeFill");Check(!timer.GetComponent<Image>().raycastTarget,"feedback band passes clicks through");
        }
        if(driver!=null && driver.ImplementationStage>=4)
        {
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();var root=(RectTransform)canvas.transform;var main=Camera.main;
            var clear=UnityEngine.Object.FindFirstObjectByType<ScreenClearCamera>();var clearView=clear!=null?clear.GetComponent<Camera>():null;
            Check(clearView!=null && main!=null && clearView!=main && clearView.rect==new Rect(0,0,1,1) && clearView.depth<main.depth && clearView.cullingMask==0 && clearView.clearFlags==CameraClearFlags.SolidColor,"full-screen clear behind the map viewport");
            var scaler=canvas.GetComponent<CanvasScaler>();Check(scaler.uiScaleMode==CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.referenceResolution==new Vector2(1920,1080),"one 1920x1080 layout scales to 1280x720");
            var names=canvas.GetComponentsInChildren<Button>(true).Select(b=>b.name).ToArray();
            foreach(string name in new[]{"StartBusiness","DispatchRoute","DispatchSuggestion","ReplayDay","Continue","AdvancePage"})Check(names.Contains(name),"primary action exists "+name);
            foreach(var view in canvas.GetComponentsInChildren<CourierAbilitiesView>(true).Where(v=>v==v.GetComponent<CourierAbilitiesView>()))
            {
                var labels=new SerializedObject(view).FindProperty("labels");
                for(int i=0;i<labels.arraySize;i++)Check(UIThemeDriver.SizeFor((TMP_Text)labels.GetArrayElementAtIndex(i).objectReferenceValue)==ThemeTextSize.Caption,"ability rows stay caption size "+view.name+" / "+i);
            }
            var corners=new Vector3[4];
            foreach(string name in new[]{"Selections","CourierViewport","MapViewportFrame","PlanningDrawer","Preparation","ResultCard","SaveChoice","PauseMenu","HelpOverlay","ReturnTitleConfirmation"})
            {
                var rect=(RectTransform)UIVisibility.Find(root,name);rect.GetWorldCorners(corners);
                Check(corners.Select(c=>root.InverseTransformPoint(c)).All(c=>root.rect.xMin-2<=c.x && c.x<=root.rect.xMax+2 && root.rect.yMin-2<=c.y && c.y<=root.rect.yMax+2),"screen bounds "+name);
            }
            Check(UIVisibility.Find(root,"LedgerViewport").GetComponent<ScrollRect>().vertical,"settlement remains scrollable");
            string report=LayoutReport();Check(report.Length==0,"current screen has no overflow or overlap: "+report);
        }
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
            // Scrolled lists and the overlay layer are compared only with their own members.
            if(a.GetComponentInParent<ScrollRect>()!=b.GetComponentInParent<ScrollRect>() || InOverlay(a.transform)!=InOverlay(b.transform))continue;
            var overlap=Bounds((RectTransform)a.transform);var other=Bounds((RectTransform)b.transform);
            float w=Mathf.Min(overlap.xMax,other.xMax)-Mathf.Max(overlap.xMin,other.xMin),h=Mathf.Min(overlap.yMax,other.yMax)-Mathf.Max(overlap.yMin,other.yMin);
            if(w>1 && h>1)issues.Add("buttons overlap "+Path(a.transform)+" / "+Path(b.transform));
        }
        return string.Join("; ",issues);
    }
    private static bool InOverlay(Transform t){for(;t!=null;t=t.parent)if(t.name=="OverlayScreens")return true;return false;}
    private static bool Shown(Transform t)
    {
        if(!t.gameObject.activeInHierarchy)return false;
        for(;t!=null;t=t.parent){var group=t.GetComponent<CanvasGroup>();if(group!=null){if(group.alpha<=0)return false;if(group.ignoreParentGroups)break;}}
        return true;
    }
    private static string Path(Transform t){string path=t.name;for(int i=0;i<2 && t.parent!=null;i++){t=t.parent;path=t.name+"/"+path;}return path;}
}
