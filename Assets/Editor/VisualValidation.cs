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
            Check(canvas.GetComponentsInChildren<TMP_Text>(true).All(t=>t.font==theme.font && t.fontSize>=theme.caption),"all screens use readable Japanese theme font");
            Check(canvas.GetComponentsInChildren<Button>(true).All(b=>b.GetComponent<ThemedButton>()!=null),"all button actions retain themed component");
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
            Check(UnityEngine.Object.FindFirstObjectByType<MapVisuals>()!=null && GameObject.Find("CityBlock")!=null,"independent city decoration layer");
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
        return $"PASS: {count} visual checks.";
    }
}
