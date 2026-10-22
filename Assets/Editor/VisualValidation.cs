using System;
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
        return $"PASS: {count} visual checks.";
    }
}
