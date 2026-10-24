using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Builds the shop screens that are fixed layouts as prefabs (Assets/Prefabs/UI) and swaps them into the Game scene.
/// The view components then only fill in values; the layout is edited in Prefab Mode. Rebuilding overwrites manual edits.</summary>
public static class ScreenPrefabs
{
    public const string Folder="Assets/Prefabs/UI/";
    private static UITheme T=>UiKit.T;

    [MenuItem("Tools/Screens/Build And Install Game Over")]
    public static void GameOverMenu(){if(ConfirmRebuild("GameOverView"))Debug.Log(BuildGameOver()+" | "+Install("GameOverView"));}
    [MenuItem("Tools/Screens/Build And Install Hub")]
    public static void HubMenu(){if(ConfirmRebuild("HubView and card prefabs"))Debug.Log(BuildCards()+" | "+BuildHub()+" | "+Install("HubView"));}
    [MenuItem("Tools/Screens/Build And Install Business")]
    public static void BusinessMenu(){if(ConfirmRebuild("BusinessView"))Debug.Log(BuildBusiness()+" | "+Install("BusinessView"));}
    [MenuItem("Tools/Screens/Build And Install Settlement")]
    public static void SettlementMenu(){if(ConfirmRebuild("SettlementView"))Debug.Log(BuildSettlement()+" | "+Install("SettlementView"));}

    private static bool ConfirmRebuild(string name)
    {
        if(Application.isPlaying)
        {
            Debug.LogWarning("Exit Play Mode before rebuilding a screen.");
            return false;
        }
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if(scene.isDirty)
            {
                Debug.LogWarning("Save or discard your scene edits explicitly before rebuilding screens. No changes made.");
                return false;
            }
        }
        return EditorUtility.DisplayDialog("Rebuild "+name+"?",
            "This replaces the generated prefab layout and its scene instance. Manual prefab edits will be replaced. "
            +"The existing prefab files will be copied to Library/PrefabBackups first. "
            +"The scene will be left unsaved so you can inspect the result.","Back up and rebuild","Cancel");
    }

    /// <summary>All entry points, including automation, preserve existing prefab bytes before replacement.</summary>
    private static void BackupPrefab(string path)
    {
        if(!File.Exists(path))return;
        string folder="Library/PrefabBackups/"+System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff");
        Directory.CreateDirectory(folder);
        File.Copy(path,Path.Combine(folder,Path.GetFileName(path)));
        if(File.Exists(path+".meta"))File.Copy(path+".meta",Path.Combine(folder,Path.GetFileName(path)+".meta"));
    }

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
        if(oldGroup!=null && group!=null)
        {
            group.alpha=oldGroup.alpha;
            group.interactable=oldGroup.interactable;
            group.blocksRaycasts=oldGroup.blocksRaycasts;
        }
        // Re-point references from anywhere in the scene to the old root (its GameObject or root components, by type).
        // References to the old root or to a child with the same path in the prefab (e.g. MapViewportUI -> MapArea) move over.
        var map=new Dictionary<Object,Object>();
        foreach(var t in old.GetComponentsInChildren<Transform>(true))
        {
            string path=t==old?"":UnityEditor.AnimationUtility.CalculateTransformPath(t,old);
            var twin=path==""?instance.transform:instance.transform.Find(path);if(twin==null)continue;
            map[t.gameObject]=twin.gameObject;
            foreach(var c in t.GetComponents<Component>()){if(c==null)continue;var match=twin.GetComponent(c.GetType());if(match!=null)map[c]=match;}
        }
        int moved=0;
        foreach(var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(mb==null || mb.transform.IsChildOf(old))continue;
            var so=new SerializedObject(mb);var p=so.GetIterator();bool changed=false;
            while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue!=null && map.TryGetValue(p.objectReferenceValue,out var to))
            {
                p.objectReferenceValue=to;
                changed=true;
                moved++;
            }
            if(changed)so.ApplyModifiedProperties();
        }
        var leftover=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(mb=>mb!=null && !mb.transform.IsChildOf(old)).SelectMany(mb=>
        {
            var so=new SerializedObject(mb);
            var p=so.GetIterator();
            var l=new List<string>();
            while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue is Component c && c!=null && c.transform.IsChildOf(old))l.Add(mb.GetType().Name+"."+p.propertyPath);
            return l;
        }).ToList();
        if(leftover.Count>0)
        {
            Undo.DestroyObjectImmediate(instance);
            return "not installed: references into the old "+name+" children: "+string.Join(", ",leftover);
        }
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
        try
        {
            Directory.CreateDirectory(Folder);
            BackupPrefab(Folder+name+".prefab");
            PrefabUtility.SaveAsPrefabAsset(root,Folder+name+".prefab",out bool ok);
            return ok?"saved "+Folder+name+".prefab":"save failed "+name;
        }
        finally{Object.DestroyImmediate(root);}
    }
    private static void Set(SerializedObject so,string field,Object value)
    {
        var p=so.FindProperty(field);
        if(p==null)throw new System.Exception(so.targetObject.GetType().Name+" has no field "+field);
        p.objectReferenceValue=value;
    }
    private static void SetArray(SerializedObject so,string field,Object[] values)
    {
        var p=so.FindProperty(field);
        p.arraySize=values.Length;
        for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
    }

    /// <summary>Background images in Assets/Art/Screens as uncompressed sprites (gradients band when compressed).</summary>
    private static Sprite ScreenArt(string name)
    {
        string path="Assets/Art/Screens/"+name+".png";
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new System.Exception("missing "+path+" (run Tools/Art/title_art.py)");
        if(importer.textureType!=TextureImporterType.Sprite || importer.textureCompression!=TextureImporterCompression.Uncompressed)
        {
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---- hub cards (Assets/Prefabs/UI/Cards): one prefab per kind, instantiated by HubView for each person or facility ----
    public const string CardFolder=Folder+"Cards/";
    private const float CardW=356,CardH=610;
    private static GameObject CardRoot(string name,float w,float h,out RectTransform rect,bool decorate=true)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;rect=(RectTransform)go.transform;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(w,h);
        if(decorate)UiKit.Decorate(rect,T.paper);
        return go;
    }
    private static string SaveCard(GameObject root,string name)
    {
        try
        {
            Directory.CreateDirectory(CardFolder);
            BackupPrefab(CardFolder+name+".prefab");
            PrefabUtility.SaveAsPrefabAsset(root,CardFolder+name+".prefab",out bool ok);
            return ok?name:"FAILED "+name;
        }
        finally{Object.DestroyImmediate(root);}
    }
    private static void Header(SerializedProperty header,RectTransform card)
    {
        header.FindPropertyRelative("avatar").objectReferenceValue=UiKit.Avatar(card,"Avatar",20,20,92,T.info,"?");
        header.FindPropertyRelative("nameText").objectReferenceValue=UiKit.Label(card,"Name","",126,14,214,54,34,T.line,FontRole.Display);
        header.FindPropertyRelative("firstBadge").objectReferenceValue=UiKit.Badge(card,"Badge","",126,72,T.paper,T.line,30,16);
        header.FindPropertyRelative("secondBadge").objectReferenceValue=UiKit.Badge(card,"Badge2","",210,72,T.paper,T.line,30,16);
    }
    private static void AbilityRow(SerializedProperty row,RectTransform card,string suffix,float y,Color fill,bool marker)
    {
        row.FindPropertyRelative("label").objectReferenceValue=UiKit.Label(card,"AbilityName"+suffix,"",22,y,84,28,18,T.line,FontRole.Black);
        row.FindPropertyRelative("bar").objectReferenceValue=UiKit.Bar(card,"AbilityBar"+suffix,104,y+5,182,20,fill,marker);
        row.FindPropertyRelative("value").objectReferenceValue=UiKit.Label(card,"AbilityValue"+suffix,"",288,y,50,28,20,T.line,FontRole.Bold,TextAlignmentOptions.MidlineRight);
    }
    private static void Abilities(SerializedObject so,RectTransform card)
    {
        var list=so.FindProperty("abilities");list.arraySize=5;
        for(int i=0;i<5;i++)AbilityRow(list.GetArrayElementAtIndex(i),card,i.ToString(),130+i*34,T.barFill,true);
    }
    public static string BuildCards()
    {
        var log=new List<string>();
        {   // courier on the team
            var go=CardRoot("CourierCard",CardW,CardH,out var card);var view=go.AddComponent<CourierCardView>();var so=new SerializedObject(view);
            Header(so.FindProperty("header"),card);Abilities(so,card);
            UiKit.Label(card,"CapacityName",ShopStrings.Ability(CourierAbility.Capacity),22,300,84,28,18,T.line,FontRole.Black);
            Set(so,"capacitySlots",UiKit.Box(card,"CapacitySlots",104,302,180,26));
            Set(so,"capacityValue",UiKit.Label(card,"CapacityValue","",288,300,50,28,20,T.line,FontRole.Bold,TextAlignmentOptions.MidlineRight));
            var box=UiKit.Card(card,"TrainingBox",20,340,316,62,T.cheeseLight,10,0,2);Set(so,"trainingBox",box);
            Set(so,"trainingTitle",UiKit.Label(box,"Title","",14,4,290,28,18,T.line,FontRole.Black));
            Set(so,"trainingDetail",UiKit.Label(box,"Detail","",14,30,290,26,16,T.inkSub,FontRole.Bold));
            var xp=UiKit.Box(card,"Experience",0,340,CardW,30);Set(so,"experienceRow",xp);
            Set(so,"experienceName",UiKit.Label(xp,"XpName","",22,0,110,26,16,T.inkSub,FontRole.Black));
            Set(so,"experienceBar",UiKit.Bar(xp,"XpBar",132,6,110,16,T.basil));
            Set(so,"experienceNext",UiKit.Label(xp,"XpNext","",248,0,96,26,15,T.inkSub,FontRole.Bold));
            UiKit.DashedLine(card,20,414,316,T.disabled);
            UiKit.Label(card,"WageName",ShopStrings.Wage,22,430,100,44,20,T.line,FontRole.Black);
            Set(so,"wage",UiKit.Label(card,"Wage","",130,424,206,52,36,T.line,FontRole.Display,TextAlignmentOptions.MidlineRight));
            Set(so,"train",UiKit.Button(card,"Train",ShopStrings.Train,StickerKind.Secondary,20,CardH-96,180,70,24,null));
            Set(so,"cancelTraining",UiKit.Button(card,"CancelTraining",ShopStrings.CancelTraining,StickerKind.Secondary,20,CardH-96,180,70,22,null));
            Set(so,"fire",UiKit.Button(card,"Fire",ShopStrings.Fire,StickerKind.Danger,212,CardH-96,124,70,24,null));
            so.ApplyModifiedPropertiesWithoutUndo();log.Add(SaveCard(go,"CourierCard"));
        }
        {   // courier applicant
            var go=CardRoot("ApplicantCard",CardW,CardH,out var card);var view=go.AddComponent<ApplicantCardView>();var so=new SerializedObject(view);
            Header(so.FindProperty("header"),card);Abilities(so,card);
            UiKit.Label(card,"CapacityName",ShopStrings.Ability(CourierAbility.Capacity),22,300,84,28,18,T.line,FontRole.Black);
            Set(so,"capacityValue",UiKit.Label(card,"CapacityValue","",104,300,232,28,18,T.line,FontRole.Bold));
            Set(so,"strengths",UiKit.Label(card,"Strengths","",22,340,316,28,18,T.basil,FontRole.Black));
            UiKit.DashedLine(card,20,384,316,T.disabled);
            Set(so,"wage",UiKit.Label(card,"WageName","",22,396,316,34,20,T.line,FontRole.Black));
            Set(so,"hireCost",UiKit.Label(card,"HireCost","",22,430,316,34,20,T.inkSub,FontRole.Black));
            Set(so,"reason",UiKit.Label(card,"Reason","",22,CardH-140,316,36,15,T.berry,FontRole.Bold,TextAlignmentOptions.MidlineLeft,true));
            Set(so,"hire",UiKit.Button(card,"Hire",ShopStrings.Hire,StickerKind.Primary,20,CardH-96,316,70,26,null));
            so.ApplyModifiedPropertiesWithoutUndo();log.Add(SaveCard(go,"ApplicantCard"));
        }
        {   // kitchen staff (team member or applicant)
            var go=CardRoot("KitchenCard",CardW,CardH,out var card);var view=go.AddComponent<KitchenCardView>();var so=new SerializedObject(view);
            Header(so.FindProperty("header"),card);
            AbilityRow(so.FindProperty("speed"),card,"Speed",130,T.info,false);AbilityRow(so.FindProperty("care"),card,"Care",164,T.info,false);
            var info=UiKit.Card(card,"Info",20,214,316,110,T.infoLight,10,0,2,T.info);
            Set(so,"info",UiKit.Label(info,"Text","",14,8,288,96,16,T.line,FontRole.Bold,TextAlignmentOptions.TopLeft,true));
            UiKit.DashedLine(card,20,346,316,T.disabled);
            UiKit.Label(card,"WageName",ShopStrings.Wage,22,362,100,44,20,T.line,FontRole.Black);
            Set(so,"wage",UiKit.Label(card,"Wage","",130,356,206,52,36,T.line,FontRole.Display,TextAlignmentOptions.MidlineRight));
            Set(so,"hireCost",UiKit.Label(card,"HireCost","",22,416,316,30,18,T.inkSub,FontRole.Black));
            Set(so,"reason",UiKit.Label(card,"Reason","",22,CardH-140,316,36,15,T.berry,FontRole.Bold,TextAlignmentOptions.MidlineLeft,true));
            Set(so,"hire",UiKit.Button(card,"Hire",ShopStrings.Hire,StickerKind.Primary,20,CardH-96,316,70,26,null));
            Set(so,"trainLater",UiKit.Button(card,"TrainLater","訓練",StickerKind.Secondary,20,CardH-96,180,70,20,null));
            Set(so,"fire",UiKit.Button(card,"Fire",ShopStrings.Fire,StickerKind.Danger,212,CardH-96,124,70,24,null));
            so.ApplyModifiedPropertiesWithoutUndo();log.Add(SaveCard(go,"KitchenCard"));
        }
        {   // facility
            const float w=300;
            var go=CardRoot("FacilityCard",w,CardH,out var card);
            var view=go.AddComponent<FacilityCardView>();
            var so=new SerializedObject(view);
            var picture=UiKit.Box(card,"Picture",w/2-56,22,112,112).gameObject.AddComponent<Image>();
            picture.raycastTarget=false;
            picture.preserveAspect=true;
            Set(so,"picture",picture);
            Set(so,"title",UiKit.Label(card,"Title","",0,146,w,44,30,T.line,FontRole.Display,TextAlignmentOptions.Center));
            Set(so,"level",UiKit.Badge(card,"Level","",0,196,T.cheeseLight,T.line,32,18,120));
            Set(so,"effect",UiKit.Label(card,"Effect","",20,244,w-40,80,19,T.line,FontRole.Bold,TextAlignmentOptions.Top,true));
            Set(so,"price",UiKit.Label(card,"Price","",20,340,w-40,36,22,T.line,FontRole.Black,TextAlignmentOptions.Center));
            Set(so,"maintenance",UiKit.Label(card,"Maintenance","",20,380,w-40,30,18,T.inkSub,FontRole.Bold,TextAlignmentOptions.Center));
            Set(so,"reason",UiKit.Label(card,"Reason","",20,CardH-140,w-40,36,15,T.berry,FontRole.Bold,TextAlignmentOptions.Center,true));
            Set(so,"buy",UiKit.Button(card,"Buy","購入",StickerKind.Primary,20,CardH-96,w-40,70,26,null));
            SetArray(so,"tiers",Enumerable.Range(1,3).Select(t=>(Object)UiKit.Button(card,"Ad"+t,"",StickerKind.Secondary,20,330+(t-1)*86,w-40,68,20,null)).ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();log.Add(SaveCard(go,"FacilityCard"));
        }
        {   // the dashed "find a teammate" card
            var go=CardRoot("FindTeammateCard",CardW,CardH,out var card,false);
            var view=go.AddComponent<FindTeammateCardView>();
            var so=new SerializedObject(view);
            UiKit.Fill(UiKit.Stretch(card,"Face"),new Color(T.paper.r,T.paper.g,T.paper.b,.45f),T.cardRadius);UiKit.Dashed(card,T.line,16,10,3);
            var plus=UiKit.Box(card,"Plus",CardW/2-50,170,100,100);
            UiKit.Dashed(plus,T.line,10,8,3);
            UiKit.Icon(plus,"Icon","icon_plus",25,25,50,T.line);
            UiKit.Label(card,"Title",ShopStrings.FindTeammate,0,290,CardW,50,32,T.line,FontRole.Display,TextAlignmentOptions.Center);
            Set(so,"detail",UiKit.Label(card,"Detail","",0,350,CardW,64,18,T.inkSub,FontRole.Black,TextAlignmentOptions.Center,true));
            Set(so,"seeApplicants",UiKit.Button(card,"SeeApplicants",ShopStrings.SeeApplicants,StickerKind.Secondary,58,430,240,70,24,null));
            so.ApplyModifiedPropertiesWithoutUndo();log.Add(SaveCard(go,"FindTeammateCard"));
        }
        {   // management tab
            var go=CardRoot("ManagementPanel",1588,CardH,out var panel,false);
            var view=go.AddComponent<ManagementPanelView>();
            var so=new SerializedObject(view);
            var policy=UiKit.Card(panel,"Policy",0,0,620,CardH,T.paper);
            UiKit.Label(policy,"Title",ShopStrings.Promise,28,24,560,48,32,T.line,FontRole.Display);
            Set(so,"promise",UiKit.Slider(policy,"PromiseSlider",34,124,390,36,T.cheese,null));
            Set(so,"promiseValue",UiKit.Label(policy,"Value","",436,94,160,84,56,T.line,FontRole.Display,TextAlignmentOptions.Center));
            Set(so,"promiseDetail",UiKit.Label(policy,"Detail","",28,206,564,200,20,T.line,FontRole.Bold,TextAlignmentOptions.TopLeft,true));
            var history=UiKit.Card(panel,"History",650,0,938,CardH,T.paper);
            UiKit.Label(history,"Title","これまでの営業",28,24,600,48,32,T.line,FontRole.Display);
            Set(so,"totals",UiKit.Label(history,"Totals","",28,80,880,32,20,T.inkSub,FontRole.Black));
            Set(so,"empty",UiKit.Label(history,"Empty","まだ記録はありません。営業を始めましょう。",28,140,880,40,22,T.inkSub,FontRole.Black));
            Set(so,"rows",UiKit.Box(history,"Rows",28,136,880,440));
            so.ApplyModifiedPropertiesWithoutUndo();log.Add(SaveCard(go,"ManagementPanel"));
        }
        return "cards: "+string.Join(",",log);
    }
    private static T Card<T>(string name) where T:Component=>AssetDatabase.LoadAssetAtPath<GameObject>(CardFolder+name+".prefab")?.GetComponent<T>();

    // ---- 営業中の店内 (厨房・指示盤): the fixed layout from BusinessView.BuildLayout (needs the Game scene open) ----
    public static string BuildBusiness()
    {
        if(Object.FindFirstObjectByType<CookingManager>(FindObjectsInactive.Include)==null)return "open the Game scene first (the layout reads its managers)";
        var go=Root("BusinessView",out var root);var view=go.AddComponent<BusinessView>();
        view.BuildLayout();
        var missing=TitleValidation.MissingReferences(view);
        if(missing.Length>0){Object.DestroyImmediate(go);return "BusinessView layout left references empty: "+missing;}
        string saved=Save(go,"BusinessView");
        // Serialized references can be dropped on save (e.g. a component whose script file name differs from its class).
        var asset=AssetDatabase.LoadAssetAtPath<BusinessView>(Folder+"BusinessView.prefab");
        string lost=asset!=null?TitleValidation.MissingReferences(asset):"prefab missing";
        return lost.Length>0?saved+" BUT references lost after saving: "+lost:saved;
    }

    // ---- 開店準備の事務室 (4-2): the frame; the cards in Content are built by HubView ----
    public static string BuildHub()
    {
        var go=Root("HubView",out var root);var view=go.AddComponent<HubView>();
        var bg=UiKit.Stretch(root,"Background");UiKit.Fill(bg,T.floorA,0,true);
        for(int y=140;y<1080;y+=120){var line=UiKit.Box(bg,"Stripe",0,y,1920,3);UiKit.Fill(line,new Color(T.floorB.r,T.floorB.g,T.floorB.b,.6f),0);}
        var top=UiKit.Card(root,"TopBar",32,24,1856,92,T.paper);
        UiKit.Icon(top,"Sun","icon_sun",30,24,44,T.line);
        var day=UiKit.Label(top,"Day","1日目",86,10,180,72,40,T.line,FontRole.Display);
        var phase=UiKit.Label(top,"Phase",ShopStrings.Preparation,230,30,150,36,20,T.inkSub,FontRole.Black);
        UiKit.Icon(top,"Coin","icon_coin",400,24,44,T.line);
        var cash=UiKit.Label(top,"Cash","¥0",456,10,260,72,40,T.line,FontRole.Display);
        var cashDelta=UiKit.Badge(top,"CashDelta","▲ +0",700,30,T.basil,Color.white,34,18);
        UiKit.Icon(top,"Star","icon_star",880,24,44,T.line);
        var reputation=UiKit.Label(top,"Reputation","50",936,10,90,72,38,T.line,FontRole.Display);
        var reputationBar=UiKit.Bar(top,"ReputationBar",1020,34,150,24,T.cheese);
        var message=UiKit.Label(top,"Message","",1200,20,560,52,18,T.inkSub,FontRole.Bold,TextAlignmentOptions.MidlineRight,true);
        var menuButton=UiKit.Button(top,"MenuButton","",StickerKind.Secondary,1774,12,64,64,20,null,FontRole.Black,"icon_menu");
        string[] names={ShopStrings.Staff,ShopStrings.Applicants,ShopStrings.Facilities,ShopStrings.Management};
        var tabs=new StickerButton[4];var badges=new RectTransform[4];
        for(int i=0;i<4;i++)
        {
            tabs[i]=UiKit.Button(root,"Tab"+i,names[i],StickerKind.Secondary,32,156+i*104,236,82,32,null,FontRole.Display);
            tabs[i].Label.alignment=TextAlignmentOptions.MidlineLeft;
            tabs[i].Label.rectTransform.offsetMin=new Vector2(24,0);
            tabs[i].Label.fontSize=tabs[i].Label.fontSizeMax=i<2?28:32;
            if(i<2)badges[i]=UiKit.Badge(tabs[i].Face,"Count","0",150,26,T.paper,T.line,30,16,56);
        }
        tabs[0].Kind=StickerKind.Primary;
        var note=UiKit.Label(root,"TeamNote","",32,640,236,120,17,T.inkSub,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        var content=UiKit.Box(root,"Content",300,152,1600,624);
        // No hint row on the preparation screen (2026-10-24).
        var bottom=UiKit.Card(root,"BottomBar",300,872,1588,168,T.paper);
        UiKit.Label(bottom,"ForecastTitle","今日の見通し",30,22,300,30,18,T.inkSub,FontRole.Black);
        string[] heads={"日給の合計","固定費","維持費","訓練費"};var values=new TextMeshProUGUI[4];
        for(int i=0;i<4;i++)
        {
            UiKit.Label(bottom,"Head"+i,heads[i],30+i*150,56,150,28,17,T.inkSub,FontRole.Black);
            values[i]=UiKit.Label(bottom,"Value"+i,"¥0",30+i*150,88,150,50,32,T.line,FontRole.Display);
        }
        var divider=UiKit.Box(bottom,"Divider",660,22,3,124);UiKit.Fill(divider,T.paperDeep,0);
        UiKit.Label(bottom,"PromiseTitle",ShopStrings.Promise,700,30,200,30,18,T.inkSub,FontRole.Black);
        var slider=UiKit.Slider(bottom,"PromiseSlider",700,98,250,30,T.cheese,null);
        var promise=UiKit.Label(bottom,"Promise","60秒",960,70,140,72,40,T.line,FontRole.Display,TextAlignmentOptions.Center);
        var start=UiKit.Button(bottom,"StartBusinessV2",ShopStrings.StartBusiness+" ▶",StickerKind.Primary,1120,26,436,112,52,null,FontRole.Display);
        var dialog=UiKit.Stretch(root,"Dialog");
        UiKit.Fill(dialog,new Color(T.line.r,T.line.g,T.line.b,.45f),0,true);
        var group=UiKit.Group(dialog);
        group.alpha=0;
        group.interactable=group.blocksRaycasts=false;
        var trainingCard=UiKit.Card(dialog,"TrainingDialog",560,200,800,620,T.paper);
        var training=trainingCard.gameObject.AddComponent<TrainingDialogView>();
        var ts=new SerializedObject(training);
        Set(ts,"title",UiKit.Label(trainingCard,"Title","",36,28,700,52,36,T.line,FontRole.Display));
        Set(ts,"note",UiKit.Label(trainingCard,"Note",ShopStrings.CourierTrainingNote,36,88,720,32,20,T.inkSub,FontRole.Black));
        SetArray(ts,"abilities",Enumerable.Range(0,6).Select(i=>(Object)UiKit.Button(trainingCard,"Train"+i,"",StickerKind.Secondary,36+(i%2)*372,140+(i/2)*104,356,84,22,null)).ToArray());
        Set(ts,"close",UiKit.Button(trainingCard,"Close","やめる",StickerKind.Secondary,560,520,204,72,24,null));
        ts.ApplyModifiedPropertiesWithoutUndo();
        var so=new SerializedObject(view);
        Set(so,"trainingDialog",training);
        Set(so,"courierCard",Card<CourierCardView>("CourierCard"));
        Set(so,"applicantCard",Card<ApplicantCardView>("ApplicantCard"));
        Set(so,"kitchenCard",Card<KitchenCardView>("KitchenCard"));
        Set(so,"facilityCard",Card<FacilityCardView>("FacilityCard"));
        Set(so,"findTeammateCard",Card<FindTeammateCardView>("FindTeammateCard"));
        Set(so,"managementPanel",Card<ManagementPanelView>("ManagementPanel"));
        Set(so,"content",content);
        Set(so,"dialog",dialog);
        Set(so,"dayText",day);
        Set(so,"phaseText",phase);
        Set(so,"cashText",cash);
        Set(so,"reputationText",reputation);
        Set(so,"noteText",note);
        Set(so,"forecastWages",values[0]);
        Set(so,"forecastFixed",values[1]);
        Set(so,"forecastMaintenance",values[2]);
        Set(so,"forecastTraining",values[3]);
        Set(so,"promiseText",promise);Set(so,"messageText",message);Set(so,"cashDelta",cashDelta);Set(so,"reputationBar",reputationBar);
        SetArray(so,"tabs",tabs);
        SetArray(so,"tabBadges",badges);
        Set(so,"startButton",start);
        Set(so,"menuButton",menuButton);
        Set(so,"promiseSlider",slider);
        so.ApplyModifiedPropertiesWithoutUndo();
        return Save(go,"HubView");
    }

    // ---- 今日のまとめ (4-3): the frame; the receipt is built by SettlementView each day ----
    public static string BuildSettlement()
    {
        var go=Root("SettlementView",out var root);var view=go.AddComponent<SettlementView>();
        var sky=UiKit.Stretch(root,"Sky").gameObject.AddComponent<Image>();sky.sprite=ScreenArt("settlement_sky");sky.raycastTarget=true;
        var sun=UiKit.Box(root,"Sun",1480,120,240,240);
        var s=sun.gameObject.AddComponent<Image>();
        s.sprite=UiKit.Circle;
        s.color=new Color(1,.92f,.70f,.9f);
        s.raycastTarget=false;
        var city=UiKit.Box(root,"Skyline",0,900,1920,180);
        float[] xs={0,90,240,400,520,610,720,860,990,1110,1200,1390,1500,1720,1800};
        float[] hs={150,110,140,80,120,70,140,110,60,140,90,130,70,120,90};
        for(int i=0;i<xs.Length;i++)
        {
            var b=UiKit.Box(city,"B"+i,xs[i],180-hs[i],(i+1<xs.Length?xs[i+1]-xs[i]:120)+10,hs[i]);
            UiKit.Fill(b,new Color(.62f,.30f,.25f,.55f+.1f*(i%2)),0);
        }
        var title=UiKit.Label(root,"Title","",120,48,1100,96,64,T.line,FontRole.Display);
        var badge=UiKit.Badge(root,"Closed",ShopStrings.Ended,760,76,T.paper,T.line,38,18);
        var legend=UiKit.Box(root,"Legend",120,150,700,40);
        string[] names={"売上","コスト","損益","成長と評判"};float x=0;var dots=new Image[4];
        for(int i=0;i<4;i++)
        {
            var dot=UiKit.Box(legend,"Dot"+i,x,4,28,28);
            var e=dot.gameObject.AddComponent<Image>();
            e.sprite=UiKit.Circle;
            e.color=T.line;
            e.raycastTarget=false;
            dots[i]=UiKit.Stretch(dot,"Fill",3,3,3,3).gameObject.AddComponent<Image>();
            dots[i].sprite=UiKit.Circle;
            dots[i].color=T.paper;
            dots[i].raycastTarget=false;
            var l=UiKit.Label(legend,"Name"+i,names[i],x+36,0,160,36,20,T.line,FontRole.Black);x+=36+l.GetPreferredValues().x+28;
        }
        var city2=UiKit.Label(root,"CityTomorrow","",120,962,1050,48,T.bodySize,T.line,FontRole.Black);
        var skip=UiKit.Button(root,"Skip","スキップ",StickerKind.Secondary,1296,896,150,80,24,null);
        var next=UiKit.Button(root,"NextDay","次の日へ ▶",StickerKind.Primary,1460,880,340,104,42,null,FontRole.Display);
        var so=new SerializedObject(view);
        Set(so,"title",title);
        Set(so,"cityTomorrow",city2);
        Set(so,"titleBadge",badge);
        SetArray(so,"legendDots",dots);
        Set(so,"nextButton",next);
        Set(so,"skipButton",skip);
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
        for(int i=0;i<3;i++)
        {
            var c=UiKit.Card(card,"Stat"+i,60+i*300,210,280,150,T.paperDeep,16,0,2);
            UiKit.Label(c,"Head",heads[i],0,18,280,32,20,T.inkSub,FontRole.Black,TextAlignmentOptions.Center);
            values[i]=UiKit.Label(c,"Value","",0,58,280,70,44,T.line,FontRole.Display,TextAlignmentOptions.Center);
        }
        UiKit.Label(card,"TeamTitle","育てたスタッフ",60,392,600,40,26,T.line,FontRole.Display);
        var team=UiKit.Box(card,"Team",60,444,880,116);
        var records=UiKit.Label(card,"Records","",60,566,880,36,18,T.inkSub,FontRole.Black);
        var retry=UiKit.Button(card,"Retry","もう一度",StickerKind.Primary,520,620,420,100,40,null,FontRole.Display);
        var toTitle=UiKit.Button(card,"ToTitle","タイトルへ",StickerKind.Secondary,60,630,300,84,28,null,FontRole.Display);
        var so=new SerializedObject(view);
        Set(so,"days",days);
        SetArray(so,"statValues",values);
        Set(so,"team",team);
        Set(so,"records",records);
        Set(so,"retry",retry);
        Set(so,"toTitle",toTitle);
        so.ApplyModifiedPropertiesWithoutUndo();
        return Save(go,"GameOverView");
    }
}
