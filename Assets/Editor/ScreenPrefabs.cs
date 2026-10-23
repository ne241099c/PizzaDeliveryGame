using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds the shop screens that are fixed layouts as prefabs (Assets/Prefabs/UI) and swaps them into SampleScene.
/// The view components then only fill in values; the layout is edited in Prefab Mode. Rebuilding overwrites manual edits.</summary>
public static class ScreenPrefabs
{
    public const string Folder="Assets/Prefabs/UI/";
    private static UITheme T=>UiKit.T;

    [MenuItem("Tools/Screens/Build And Install Game Over")]
    public static void GameOverMenu()=>Debug.Log(BuildGameOver()+" | "+Install("GameOverView"));
    [MenuItem("Tools/Screens/Build And Install Settlement")]
    public static void SettlementMenu()=>Debug.Log(BuildSettlement()+" | "+Install("SettlementView"));

    /// <summary>Replaces the scene object of the same name under ShopUI with the prefab, keeping its place, its CanvasGroup
    /// state and every serialized reference to it (the router's screen list, for example).</summary>
    public static string Install(string name)
    {
        if(Application.isPlaying)return "exit Play Mode first";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+name+".prefab");if(prefab==null)return "missing "+Folder+name+".prefab";
        var shop=Object.FindFirstObjectByType<ShopUiRoot>(FindObjectsInactive.Include);if(shop==null)return "missing ShopUiRoot";
        var old=shop.transform.Find(name);if(old==null)return "missing ShopUI/"+name;
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,shop.transform);Undo.RegisterCreatedObjectUndo(instance,"Install "+name);
        instance.transform.SetSiblingIndex(old.GetSiblingIndex());
        var oldGroup=old.GetComponent<CanvasGroup>();var group=instance.GetComponent<CanvasGroup>();
        if(oldGroup!=null && group!=null){group.alpha=oldGroup.alpha;group.interactable=oldGroup.interactable;group.blocksRaycasts=oldGroup.blocksRaycasts;}
        // Re-point references from anywhere in the scene to the old root (its GameObject or root components, by type).
        var map=new Dictionary<Object,Object>{{old.gameObject,instance}};
        foreach(var c in old.GetComponents<Component>()){var match=instance.GetComponent(c.GetType());if(match!=null)map[c]=match;}
        int moved=0;
        foreach(var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(mb==null || mb.transform.IsChildOf(old))continue;
            var so=new SerializedObject(mb);var p=so.GetIterator();bool changed=false;
            while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue!=null && map.TryGetValue(p.objectReferenceValue,out var to)){p.objectReferenceValue=to;changed=true;moved++;}
            if(changed)so.ApplyModifiedProperties();
        }
        var leftover=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(mb=>mb!=null && !mb.transform.IsChildOf(old)).SelectMany(mb=>{var so=new SerializedObject(mb);var p=so.GetIterator();var l=new List<string>();while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue is Component c && c!=null && c.transform.IsChildOf(old))l.Add(mb.GetType().Name+"."+p.propertyPath);return l;}).ToList();
        if(leftover.Count>0){Undo.DestroyObjectImmediate(instance);return "not installed: references into the old "+name+" children: "+string.Join(", ",leftover);}
        Undo.DestroyObjectImmediate(old.gameObject);instance.name=name;
        EditorSceneManager.MarkSceneDirty(instance.scene);
        return $"installed {name} ({moved} references moved)";
    }

    private static GameObject Root(string name,out RectTransform rect)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;rect=(RectTransform)go.transform;
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        go.AddComponent<CanvasGroup>();return go;
    }
    private static string Save(GameObject root,string name)
    {
        try{Directory.CreateDirectory(Folder);PrefabUtility.SaveAsPrefabAsset(root,Folder+name+".prefab",out bool ok);AssetDatabase.SaveAssets();return ok?"saved "+Folder+name+".prefab":"save failed "+name;}
        finally{Object.DestroyImmediate(root);}
    }
    private static void Set(SerializedObject so,string field,Object value){var p=so.FindProperty(field);if(p==null)throw new System.Exception(so.targetObject.GetType().Name+" has no field "+field);p.objectReferenceValue=value;}
    private static void SetArray(SerializedObject so,string field,Object[] values){var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}

    /// <summary>Background images in Assets/Art/Screens as uncompressed sprites (gradients band when compressed).</summary>
    private static Sprite ScreenArt(string name)
    {
        string path="Assets/Art/Screens/"+name+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new System.Exception("missing "+path+" (run Tools/Art/title_art.py)");
        if(importer.textureType!=TextureImporterType.Sprite || importer.textureCompression!=TextureImporterCompression.Uncompressed)
        {importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---- 今日のまとめ (4-3): the frame; the receipt is built by SettlementView each day ----
    public static string BuildSettlement()
    {
        var go=Root("SettlementView",out var root);var view=go.AddComponent<SettlementView>();
        var sky=UiKit.Stretch(root,"Sky").gameObject.AddComponent<Image>();sky.sprite=ScreenArt("settlement_sky");sky.raycastTarget=true;
        var sun=UiKit.Box(root,"Sun",1480,120,240,240);var s=sun.gameObject.AddComponent<Image>();s.sprite=UiKit.Circle;s.color=new Color(1,.92f,.70f,.9f);s.raycastTarget=false;
        var city=UiKit.Box(root,"Skyline",0,900,1920,180);
        float[] xs={0,90,240,400,520,610,720,860,990,1110,1200,1390,1500,1720,1800};float[] hs={150,110,140,80,120,70,140,110,60,140,90,130,70,120,90};
        for(int i=0;i<xs.Length;i++){var b=UiKit.Box(city,"B"+i,xs[i],180-hs[i],(i+1<xs.Length?xs[i+1]-xs[i]:120)+10,hs[i]);UiKit.Fill(b,new Color(.62f,.30f,.25f,.55f+.1f*(i%2)),0);}
        var title=UiKit.Label(root,"Title","",120,48,1100,96,64,T.line,FontRole.Display);
        var badge=UiKit.Badge(root,"Closed",ShopStrings.Ended,760,76,T.paper,T.line,38,18);
        var legend=UiKit.Box(root,"Legend",120,150,700,40);
        string[] names={"売上","コスト","損益","成長と評判"};float x=0;var dots=new Image[4];
        for(int i=0;i<4;i++)
        {
            var dot=UiKit.Box(legend,"Dot"+i,x,4,28,28);var e=dot.gameObject.AddComponent<Image>();e.sprite=UiKit.Circle;e.color=T.line;e.raycastTarget=false;
            dots[i]=UiKit.Stretch(dot,"Fill",3,3,3,3).gameObject.AddComponent<Image>();dots[i].sprite=UiKit.Circle;dots[i].color=T.paper;dots[i].raycastTarget=false;
            var l=UiKit.Label(legend,"Name"+i,names[i],x+36,0,160,36,20,T.line,FontRole.Black);x+=36+l.GetPreferredValues().x+28;
        }
        var city2=UiKit.Label(root,"CityTomorrow","",120,962,1050,48,T.bodySize,T.line,FontRole.Black);
        var skip=UiKit.Button(root,"Skip","スキップ",StickerKind.Secondary,1296,896,150,80,24,null);
        var next=UiKit.Button(root,"NextDay","次の日へ ▶",StickerKind.Primary,1460,880,340,104,42,null,FontRole.Display);
        var so=new SerializedObject(view);
        Set(so,"title",title);Set(so,"cityTomorrow",city2);Set(so,"titleBadge",badge);SetArray(so,"legendDots",dots);Set(so,"nextButton",next);Set(so,"skipButton",skip);
        so.ApplyModifiedPropertiesWithoutUndo();
        return Save(go,"SettlementView");
    }

    // ---- 閉店 (4-4) ----
    public static string BuildGameOver()
    {
        var go=Root("GameOverView",out var root);var view=go.AddComponent<GameOverView>();
        UiKit.Fill(UiKit.Stretch(root,"Background"),new Color(.36f,.27f,.22f),0,true);
        for(int y=0;y<1080;y+=96){var s=UiKit.Box(root,"Stripe",0,y,1920,3);UiKit.Fill(s,new Color(1,1,1,.05f),0);}
        var card=UiKit.Card(root,"Card",460,150,1000,760,T.paper,24,8,3);
        UiKit.Label(card,"Title","閉店しました",0,42,1000,98,64,T.line,FontRole.Display,TextAlignmentOptions.Center);
        var days=UiKit.Label(card,"Days","",0,140,1000,40,26,T.inkSub,FontRole.Black,TextAlignmentOptions.Center);
        string[] heads={"通算売上","最高の日の売上","配達した数"};var values=new TextMeshProUGUI[3];
        for(int i=0;i<3;i++){var c=UiKit.Card(card,"Stat"+i,60+i*300,210,280,150,T.paperDeep,16,0,2);UiKit.Label(c,"Head",heads[i],0,18,280,32,20,T.inkSub,FontRole.Black,TextAlignmentOptions.Center);values[i]=UiKit.Label(c,"Value","",0,58,280,70,44,T.line,FontRole.Display,TextAlignmentOptions.Center);}
        UiKit.Label(card,"TeamTitle","育てたスタッフ",60,392,600,40,26,T.line,FontRole.Display);
        var team=UiKit.Box(card,"Team",60,444,880,116);
        var records=UiKit.Label(card,"Records","",60,566,880,36,18,T.inkSub,FontRole.Black);
        var retry=UiKit.Button(card,"Retry","もう一度",StickerKind.Primary,520,620,420,100,40,null,FontRole.Display);
        var toTitle=UiKit.Button(card,"ToTitle","タイトルへ",StickerKind.Secondary,60,630,300,84,28,null,FontRole.Display);
        var so=new SerializedObject(view);
        Set(so,"days",days);SetArray(so,"statValues",values);Set(so,"team",team);Set(so,"records",records);Set(so,"retry",retry);Set(so,"toTitle",toTitle);
        so.ApplyModifiedPropertiesWithoutUndo();
        return Save(go,"GameOverView");
    }
}
