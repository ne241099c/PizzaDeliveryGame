using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds the title screen prefab (D-5a) from the art in Assets/Art/Title and installs it in SampleScene.
/// Run once (Tools/Title/Build Prefab, then Tools/Title/Install In Scene); afterwards the prefab is the source and can be
/// edited in Prefab Mode. Rebuilding overwrites manual prefab edits, so do that only to start over.</summary>
public static class TitleViewBuilder
{
    public const string PrefabPath="Assets/Prefabs/UI/TitleView.prefab";
    private const string Art="Assets/Art/Title/",UiArt="Assets/Art/UI/";
    private static UITheme T=>UiKit.T;

    [System.Serializable] private class Layer {public string name;public float x,y,w,h;}
    [System.Serializable] private class Point {public float x,y;}
    [System.Serializable] private class Manifest {public Layer[] layers;public Point[] bulbs;}

    [MenuItem("Tools/Title/Build Prefab")]
    public static void BuildMenu()=>Debug.Log(Build());
    [MenuItem("Tools/Title/Install In Scene")]
    public static void InstallMenu()=>Debug.Log(Install());

    // ---- import settings and shared assets ----
    public static string ConfigureArt()
    {
        int count=0;
        foreach(var path in Directory.GetFiles(Art,"*.png").Concat(Directory.GetFiles(UiArt,"*.png")))
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            // Full-screen gradients band when compressed.
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.spriteBorder=Path.GetFileNameWithoutExtension(path)=="ui_round"?new Vector4(64,64,64,64):Vector4.zero;
            importer.SaveAndReimport();count++;
        }
        var theme=AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Resources/UITheme.asset");
        if(theme==null)return "missing Assets/Resources/UITheme.asset";
        var so=new SerializedObject(theme);
        so.FindProperty("roundSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(UiArt+"ui_round.png");
        so.FindProperty("circleSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(UiArt+"ui_circle.png");
        so.ApplyModifiedProperties();AssetDatabase.SaveAssetIfDirty(theme);
        return $"{count} sprites configured; theme round={theme.roundSprite!=null} circle={theme.circleSprite!=null}";
    }
    private static Material TextMaterial(string name,TMP_FontAsset font,float outline,Color outlineColor,float dilate)
    {
        string path=Art+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(font.material);AssetDatabase.CreateAsset(mat,path);}
        else mat.CopyPropertiesFromMaterial(font.material);
        mat.shader=font.material.shader;mat.EnableKeyword("OUTLINE_ON");
        mat.SetFloat("_OutlineWidth",outline);mat.SetColor("_OutlineColor",outlineColor);mat.SetFloat("_FaceDilate",dilate);
        EditorUtility.SetDirty(mat);return mat;
    }

    // ---- layout helpers: positions are the mock's 1920x1080 pixels, y down; anchor picks which edge they follow ----
    private enum Pin {TopLeft,BottomLeft,BottomRight,Top,Bottom,Fill}
    private static RectTransform Node(Transform parent,string name,float x,float y,float w,float h,Pin pin)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);Place(r,x,y,w,h,pin);return r;
    }
    private static void Place(RectTransform r,float x,float y,float w,float h,Pin pin)
    {
        switch(pin)
        {
            case Pin.Fill:r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=r.offsetMax=Vector2.zero;return;
            case Pin.Top:r.anchorMin=new Vector2(0,1);r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(.5f,1);r.offsetMin=new Vector2(x,-y-h);r.offsetMax=new Vector2(x+w-1920,-y);return;
            case Pin.Bottom:r.anchorMin=new Vector2(0,0);r.anchorMax=new Vector2(1,0);r.pivot=new Vector2(.5f,0);r.offsetMin=new Vector2(x,1080-y-h);r.offsetMax=new Vector2(x+w-1920,1080-y);return;
            case Pin.BottomLeft:r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,0);r.anchoredPosition=new Vector2(x,1080-y-h);break;
            case Pin.BottomRight:r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(x+w-1920,1080-y-h);break;
            default:r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);break;
        }
        r.sizeDelta=new Vector2(w,h);
    }
    private static Image Picture(RectTransform r,string sprite,float alpha=1)
    {
        var image=r.gameObject.AddComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+sprite+".png");
        image.raycastTarget=false;image.color=new Color(1,1,1,alpha);if(image.sprite==null)Debug.LogWarning("missing title art "+sprite);return image;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,string text,float x,float y,float w,float h,float size,Color color,TMP_FontAsset font,Material mat=null,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
    {
        var t=Node(parent,name,x,y,w,h,Pin.TopLeft).gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font;if(mat!=null)t.fontSharedMaterial=mat;t.fontSize=size;t.color=color;t.alignment=align;t.text=text;t.raycastTarget=false;
        t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Overflow;t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=Mathf.Max(T.captionSize,size*.5f);
        return t;
    }
    /// <summary>Display text at exactly its size (no shrinking), for the logo and the sign.</summary>
    private static TextMeshProUGUI Fixed(TextMeshProUGUI t){t.enableAutoSizing=false;return t;}
    private static RectTransform Pill(Transform parent,string name,float x,float y,float w,float h,Color fill,Pin pin)
    {
        var r=Node(parent,name,x,y,w,h,pin);var edge=Node(r,"Edge",0,0,0,0,Pin.Fill);UiKit.Fill(edge,T.line,h/2f);
        UiKit.Fill(UiKit.Stretch(edge,"Face",2,2,2,2),fill,h/2f-2);return r;
    }

    // ---- the prefab ----
    public static string Build()
    {
        string art=ConfigureArt();if(art.StartsWith("missing"))return art;
        var manifestPath=Art+"layers.json";if(!File.Exists(manifestPath))return "missing "+manifestPath+" (run Tools/Art/title_art.py)";
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
        Layer L(string n)=>manifest.layers.First(l=>l.name==n);
        TMP_FontAsset display=UiKit.Font(FontRole.Display),black=UiKit.Font(FontRole.Black),bold=UiKit.Font(FontRole.Bold);
        var logoMat=TextMaterial("LogoText",display,.42f,T.line,.12f);
        var kanaMat=TextMaterial("KanaText",black,.22f,T.line,.38f);
        var signMat=TextMaterial("SignText",display,0,T.line,.05f);

        var root=new GameObject("TitleView",typeof(RectTransform));root.layer=5;var rootRect=(RectTransform)root.transform;Place(rootRect,0,0,0,0,Pin.Fill);
        try
        {
            root.AddComponent<ThemeOptOut>();var view=root.AddComponent<TitleView>();var so=new SerializedObject(view);
            // Background layers, back to front.
            var scene=Node(rootRect,"Scene",0,0,0,0,Pin.Fill);
            Picture(Node(scene,"Sky",0,0,0,0,Pin.Fill),"title_sky");
            var town=L("title_town");Picture(Node(scene,"Town",town.x,town.y,town.w,town.h,Pin.Bottom),"title_town");
            var road=L("title_road");Picture(Node(scene,"Road",road.x,road.y,road.w,road.h,Pin.Bottom),"title_road");
            var shopL=L("title_shop");var shop=Node(scene,"Shop",shopL.x,shopL.y,shopL.w,shopL.h,Pin.BottomRight);Picture(shop,"title_shop");
            RectTransform InShop(string name,float x,float y,float w,float h)=>Node(shop,name,x-shopL.x,y-shopL.y,w,h,Pin.TopLeft);
            var windowL=L("title_window");Picture(InShop("Window",windowL.x,windowL.y,windowL.w,windowL.h),"title_window");
            var glowL=L("title_window_glow");var windowGlow=Picture(InShop("WindowGlow",glowL.x,glowL.y,glowL.w,glowL.h),"title_window_glow",.24f);
            var sign=InShop("Sign",968,138,804,154);
            Fixed(Text(sign,"Shadow","PIZZA",6,6,804,154,104,new Color32(0xB8,0x86,0x2A,255),display,signMat,TextAlignmentOptions.Center));
            Fixed(Text(sign,"Word","PIZZA",0,0,804,154,104,new Color32(0xFF,0xDF,0x80,255),display,signMat,TextAlignmentOptions.Center));
            Text(InShop("OpenTag",1580,485,172,70),"Word","OPEN",0,0,172,70,40,new Color32(0x8B,0xE0,0xA0,255),display,null,TextAlignmentOptions.Center);
            // String lights: the wire, then a glow and a bulb for each point (the glows twinkle).
            var wireL=L("title_wire");var lights=Node(scene,"Lights",0,0,1920,200,Pin.Top);
            Picture(Node(lights,"Wire",wireL.x,wireL.y,wireL.w,wireL.h,Pin.TopLeft),"title_wire");
            var glows=new Image[manifest.bulbs.Length];
            for(int i=0;i<manifest.bulbs.Length;i++)
            {
                var p=manifest.bulbs[i];glows[i]=Picture(Node(lights,"Glow"+i,p.x-40,p.y-40,80,80,Pin.TopLeft),"title_bulb_glow",.55f);
                Picture(Node(lights,"Bulb"+i,p.x-20,p.y-20,40,40,Pin.TopLeft),"title_bulb");
            }

            // Logo (text from GameInfo; Traveling small, the other words large) and the katakana reading.
            var logo=Node(rootRect,"Logo",0,0,900,560,Pin.TopLeft);
            // Mochiy Pop One lines are ~1.45x the font size tall, so each box is 1.5x; sizes match the mock's word widths.
            float[] ys={112,170,328},sizes={52,124,124};
            Color[] faces={T.paper,T.tomato,T.tomato};
            var lines=new TextMeshProUGUI[3];var shadows=new TextMeshProUGUI[3];
            for(int i=0;i<3;i++)
            {
                float h=Mathf.Ceil(sizes[i]*1.5f);var line=Node(logo,"Line"+i,100,ys[i],820,h,Pin.TopLeft);
                shadows[i]=Fixed(Text(line,"Shadow","",4,8,820,h,sizes[i],T.line,display,logoMat));
                lines[i]=Fixed(Text(line,"Face","",0,0,820,h,sizes[i],faces[i],display,logoMat));
            }
            var kana=Text(logo,"Kana","",100,500,700,44,26,T.paper,black,kanaMat);

            // Buttons (the left column, following the bottom edge).
            var menu=Node(rootRect,"Menu",100,600,560,420,Pin.BottomLeft);
            StickerButton Button(string name,string label,StickerKind kind,float x,float y,float w,float h,float size,FontRole role)
            {
                var b=UiKit.Button(menu,name,label,kind,x,y,w,h,size,null,role);return b;
            }
            var cont=Button("Continue","続きから",StickerKind.Primary,0,0,540,104,40,FontRole.Display);cont.LabelOffsetY=14;
            var summary=UiKit.Label(cont.Face,"Summary","",0,58,540,30,18,Color.white,FontRole.Bold,TextAlignmentOptions.Center);
            var newGame=Button("NewGame","最初から",StickerKind.Secondary,0,124,540,84,30,FontRole.Display);
            var tutorial=Button("Tutorial","チュートリアル",StickerKind.Secondary,0,228,540,84,30,FontRole.Display);
            var note=UiKit.Label(tutorial.Face,"Note","準備中",0,50,540,26,16,T.inkSub,FontRole.Bold,TextAlignmentOptions.Center);
            var badge=UiKit.Badge(menu,"FirstTimeBadge","はじめての人へ",392,208,T.cheese,T.line,34,18);badge.localRotation=Quaternion.Euler(0,0,-6);
            var help=Button("TitleHelp","遊び方",StickerKind.Secondary,0,332,260,72,26,FontRole.Black);
            var quit=Button("Quit","終了",StickerKind.Secondary,280,332,260,72,26,FontRole.Black);

            // Footer: version and credits (a button), best records, and a note when the save cannot be read.
            var credits=UiKit.Card(rootRect,"Credits",0,0,200,32,T.paper,16,0,2);Place(credits,100,1026,200,32,Pin.BottomLeft);
            var creditsButton=credits.gameObject.AddComponent<Button>();credits.gameObject.AddComponent<StickerCardAction>().Apply();
            var footer=Text(credits,"Text","",0,0,200,32,16,T.inkSub,bold,null,TextAlignmentOptions.Center);
            var recordsPill=Pill(rootRect,"Records",320,1026,620,32,new Color(T.paper.r,T.paper.g,T.paper.b,.92f),Pin.BottomLeft);
            var records=Text(recordsPill,"Text","",14,0,592,32,16,T.inkSub,bold);
            var statusPill=Pill(menu,"Status",0,20,540,64,T.cheeseLight,Pin.TopLeft);
            var status=Text(statusPill,"Text","",20,0,500,64,18,T.line,bold);status.textWrappingMode=TextWrappingModes.Normal;

            void Ref(string field,Object value){var p=so.FindProperty(field);if(p==null)throw new System.Exception("TitleView has no field "+field);p.objectReferenceValue=value;}
            void Refs(string field,Object[] values){var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
            Ref("continueButton",cont);Ref("newGameButton",newGame);Ref("tutorialButton",tutorial);Ref("helpButton",help);Ref("quitButton",quit);
            Ref("creditsButton",creditsButton);Ref("continueSummary",summary);Ref("tutorialNote",note);Ref("firstTimeBadge",badge);
            Refs("logoLines",lines);Refs("logoShadows",shadows);Ref("kana",kana);Ref("footer",footer);Ref("status",status);Ref("records",records);
            Refs("bulbGlows",glows);Ref("windowGlow",windowGlow);
            so.ApplyModifiedPropertiesWithoutUndo();view.ApplyGameInfo();
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath,out bool ok);AssetDatabase.SaveAssets();
            return ok?$"saved {PrefabPath} ({art})":"prefab save failed";
        }
        finally{Object.DestroyImmediate(root);}
    }

    // ---- scene installation: the prefab replaces the old title contents and takes over their references ----
    public static string Install()
    {
        if(Application.isPlaying)return "exit Play Mode first";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);if(prefab==null)return "missing "+PrefabPath+" (Tools/Title/Build Prefab)";
        var router=Object.FindFirstObjectByType<ScreenRouter>(FindObjectsInactive.Include);if(router==null)return "missing ScreenRouter in the open scene";
        var screen=router.Root(ScreenId.Title);var choice=screen!=null?screen.transform.Find("SaveChoice"):null;if(choice==null)return "missing TitleScreen/SaveChoice";
        var save=Object.FindFirstObjectByType<CampaignSaveManager>(FindObjectsInactive.Include);var menu=Object.FindFirstObjectByType<TitleMenuUI>(FindObjectsInactive.Include);
        if(save==null || menu==null)return "missing CampaignSaveManager or TitleMenuUI";
        var credits=AssetDatabase.LoadAssetAtPath<TextAsset>(GameInfoSync.CreditsAsset);if(credits==null)return "missing "+GameInfoSync.CreditsAsset+" (Tools/Game Info/Sync)";

        var removed=choice.Cast<Transform>().Select(t=>t.gameObject).ToArray();var removedNames=string.Join(",",removed.Select(r=>r.name));
        foreach(var old in removed)Undo.DestroyObjectImmediate(old);
        // The panel itself stays: CampaignSaveManager and TitleMenuUI show and hide it as before.
        var image=choice.GetComponent<Image>();if(image!=null){Undo.RecordObject(image,"Title");image.enabled=false;}
        var themed=choice.GetComponent<ThemedPanel>();if(themed!=null)Undo.DestroyObjectImmediate(themed);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,choice);Undo.RegisterCreatedObjectUndo(instance,"Title");
        Place((RectTransform)instance.transform,0,0,0,0,Pin.Fill);
        var view=instance.GetComponent<TitleView>();

        var s=new SerializedObject(save);
        s.FindProperty("continueButton").objectReferenceValue=view.ContinueButton.Button;s.FindProperty("newButton").objectReferenceValue=view.NewGameButton.Button;
        s.FindProperty("choiceText").objectReferenceValue=null;s.ApplyModifiedProperties();
        var m=new SerializedObject(menu);
        m.FindProperty("continueGame").objectReferenceValue=view.ContinueButton.Button;m.FindProperty("newGame").objectReferenceValue=view.NewGameButton.Button;
        m.FindProperty("titleHelp").objectReferenceValue=view.HelpButton.Button;m.FindProperty("quit").objectReferenceValue=view.QuitButton.Button;
        m.FindProperty("titleStatus").objectReferenceValue=null;m.FindProperty("titleView").objectReferenceValue=view;m.FindProperty("creditsText").objectReferenceValue=credits;
        m.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(choice.gameObject.scene);
        return $"installed {PrefabPath} under {choice.name}; removed {removed.Length} old objects ({removedNames})";
    }
}
