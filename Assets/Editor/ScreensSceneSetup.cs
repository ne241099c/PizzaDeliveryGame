using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static M4StageSceneSetup;

public static class ScreensSceneSetup
{
    private static Canvas Canvas => UnityEngine.Object.FindFirstObjectByType<Canvas>();
    private static GameObject Manager => GameObject.Find("GameManager");
    public static RectTransform Full(Transform parent, string name)
    {
        var found = parent.Find(name);
        var r = found != null ? (RectTransform)found : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        if (found == null) { Undo.RegisterCreatedObjectUndo(r.gameObject,"Screen root"); r.SetParent(parent,false); }
        r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        return r;
    }
    public static void Bind(UnityEngine.Object target, string property, UnityEngine.Object value)
    { var so=new SerializedObject(target);so.FindProperty(property).objectReferenceValue=value;so.ApplyModifiedProperties(); }
    public static void Place(Transform t, float x, float y, float width, float height)
    {
        var r=(RectTransform)t;Undo.RecordObject(r,"Screen layout");r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);
    }
    public static Transform Find(string name)=>UIVisibility.Find(Canvas.transform,name);
    private static void Move(Transform t, Transform parent) => Undo.SetTransformParent(t,parent,"Move existing UI");
    public static void Shell()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var canvas=Canvas;Undo.RecordObject(canvas,"Screen shell");
        var roots=Enum.GetNames(typeof(ScreenId)).Select(name=>Full(canvas.transform,name+"Screen")).ToArray();
        foreach(var root in roots)if(root.GetComponent<CanvasGroup>()==null)Undo.AddComponent<CanvasGroup>(root.gameObject);
        var shared=Full(canvas.transform,"SharedHeader");if(shared.GetComponent<CanvasGroup>()==null)shared.gameObject.AddComponent<CanvasGroup>();
        foreach(var t in canvas.transform.Cast<Transform>().ToArray())
        {
            if(roots.Contains(t) || t==shared)continue;
            var parent=t.name=="Preparation"?roots[1]:t.name=="DayResults"?roots[3]:t.name=="SaveChoice"?roots[0]:t.name=="Cash" || t.name=="CampaignDay"?shared:roots[2];
            Move(t,parent);
            if(t.name=="Preparation" || t.name=="DayResults" || t.name=="SaveChoice") {t.gameObject.SetActive(true);if(t.GetComponent<CanvasGroup>()==null)t.gameObject.AddComponent<CanvasGroup>();}
        }
        var router=Manager.GetComponent<ScreenRouter>()??Undo.AddComponent<ScreenRouter>(Manager);
        Bind(router,"campaign",Manager.GetComponent<CampaignManager>());Bind(router,"clock",Manager.GetComponent<GameClock>());Bind(router,"sharedHeader",shared.GetComponent<CanvasGroup>());
        var so=new SerializedObject(router);var array=so.FindProperty("screens");array.arraySize=roots.Length;for(int i=0;i<roots.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=roots[i].GetComponent<CanvasGroup>();so.ApplyModifiedProperties();
        foreach(var root in roots)UIVisibility.Set(root.gameObject,root.name=="HubScreen");
        UIVisibility.Set(Find("DayResults").gameObject,false);UIVisibility.Set(Find("SaveChoice").gameObject,false);
        EditorSceneManager.MarkSceneDirty(Manager.scene);
    }
    private static void Grid(Transform viewport)
    {
        var scroll=viewport.GetComponent<ScrollRect>();var content=scroll.content;
        var old=content.GetComponent<VerticalLayoutGroup>();if(old!=null)Undo.DestroyObjectImmediate(old);
        var grid=content.GetComponent<GridLayoutGroup>()??Undo.AddComponent<GridLayoutGroup>(content.gameObject);
        grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;grid.cellSize=new Vector2(680,330);grid.spacing=new Vector2(24,24);
        content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=new Vector2(0,0);
    }
    public static void Hub()
    {
        var canvas=Canvas;canvas.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1920,1080);
        var panel=Find("Preparation");Place(panel,0,88,1920,992);
        Place(Find("PreparationTitle"),32,16,1520,44);Place(Find("PreparationOverview"),32,68,1520,40);
        Place(Find("StartBusiness"),1500,900,370,64);Place(Find("EmploymentMessage"),256,812,1600,72);
        var staff=Full(panel,"StaffTab");var applicants=Full(panel,"ApplicantsTab");var management=Full(panel,"ManagementTab");
        foreach(var tab in new[]{staff,applicants,management}){Place(tab,256,150,1500,650);if(tab.GetComponent<CanvasGroup>()==null)tab.gameObject.AddComponent<CanvasGroup>();}
        Move(Find("TeamViewport"),staff);Place(Find("TeamViewport"),0,0,1440,650);Grid(Find("TeamViewport"));
        Move(Find("ApplicantsViewport"),applicants);Place(Find("ApplicantsViewport"),0,0,1440,650);Grid(Find("ApplicantsViewport"));
        UIVisibility.Set(Find("TeamTitle").gameObject,false);UIVisibility.Set(Find("ApplicantsTitle").gameObject,false);
        var buttons=new[]{M5SceneSetup.MakeButton(panel,"StaffButton","Staff",32,160,190,56),M5SceneSetup.MakeButton(panel,"ApplicantsButton","Applicants",32,232,190,56),M5SceneSetup.MakeButton(panel,"ManagementButton","Management",32,304,190,56)};
        var forecast=Text(panel,"Forecast","Today",32,902,1430,56,24);var history=Text(management,"DayHistory","Previous days",0,152,1400,490,24);
        var slot=Full(Find("BusinessScreen"),"BusinessPolicySlot");Place(slot,320,16,1280,100);
        var ui=Manager.GetComponent<HubScreenUI>()??Undo.AddComponent<HubScreenUI>(Manager);
        Bind(ui,"router",Manager.GetComponent<ScreenRouter>());Bind(ui,"campaign",Manager.GetComponent<CampaignManager>());Bind(ui,"clock",Manager.GetComponent<GameClock>());Bind(ui,"economy",Manager.GetComponent<BusinessEconomy>());Bind(ui,"roster",Manager.GetComponent<DeliveryPersonSelector>());Bind(ui,"forecast",forecast);Bind(ui,"history",history);Bind(ui,"policy",Find("DeliveryPolicy"));Bind(ui,"businessPolicySlot",slot);
        var so=new SerializedObject(ui);var tabs=so.FindProperty("tabs");tabs.arraySize=3;var bs=so.FindProperty("tabButtons");bs.arraySize=3;
        var groups=new[]{staff,applicants,management};for(int i=0;i<3;i++){tabs.GetArrayElementAtIndex(i).objectReferenceValue=groups[i].GetComponent<CanvasGroup>();bs.GetArrayElementAtIndex(i).objectReferenceValue=buttons[i];}so.ApplyModifiedProperties();
        var template=Find("EmploymentRowTemplate");var picker=template.gameObject.AddComponent<TrainingPickerUI>();var controls=template.Find("TrainingControls");var group=controls.GetComponent<CanvasGroup>();if(group==null)group=controls.gameObject.AddComponent<CanvasGroup>();
        var toggle=M5SceneSetup.MakeButton(template,"ChooseTraining","Training...",12,278,230,40);Bind(picker,"choices",group);Bind(picker,"toggle",toggle);
        Place(Find("Cash"),32,16,900,32);Place(Find("CampaignDay"),1100,16,720,32);
        foreach(var tab in groups)UIVisibility.Set(tab.gameObject,tab==staff);
        var stage=new SerializedObject(Manager.GetComponent<ScreenRouter>());stage.FindProperty("implementationStage").intValue=2;stage.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(Manager.scene);
    }
    public static void Business()
    {
        var root=Find("BusinessScreen");
        foreach(string name in new[]{"TopBackdrop","BottomBackdrop","LeftBackdrop","RightBackdrop","MapTitle"})UIVisibility.Set(Find(name).gameObject,false);
        Place(Find("Header"),0,0,1920,160);UIVisibility.Set(Find("Title").gameObject,false);
        Place(Find("BusinessClock"),32,66,260,44);Place(Find("PlannerScore"),1650,18,240,40);
        Place(Find("Selections"),24,180,280,610);Place(Find("OrdersTitle"),12,8,256,40);Place(Find("OrdersViewport"),12,54,256,540);Place(Find("OrdersEmpty"),12,80,256,88);
        var band=Find("CourierViewport");Move(band,root);Place(band,24,830,1872,230);UIVisibility.Set(Find("CouriersTitle").gameObject,false);
        Text(root,"CourierBandTitle","COURIERS / select an available courier",24,796,1800,30,22);
        var scroll=band.GetComponent<ScrollRect>();scroll.horizontal=true;scroll.vertical=false;
        var content=scroll.content;var vertical=content.GetComponent<VerticalLayoutGroup>();if(vertical!=null)Undo.DestroyObjectImmediate(vertical);
        var horizontal=content.gameObject.AddComponent<HorizontalLayoutGroup>();horizontal.spacing=16;horizontal.childControlHeight=true;horizontal.childControlWidth=true;horizontal.childForceExpandWidth=false;horizontal.childForceExpandHeight=false;
        content.anchorMin=content.anchorMax=content.pivot=new Vector2(0,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=new Vector2(0,230);
        var fitter=content.GetComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.Unconstrained;fitter.horizontalFit=ContentSizeFitter.FitMode.PreferredSize;
        var prefabPath=AssetDatabase.GetAssetPath(new SerializedObject(Manager.GetComponent<DeliveryPersonSelector>()).FindProperty("deliveryPersonItemPrefab").objectReferenceValue);
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);try{var layout=prefab.GetComponent<LayoutElement>()??prefab.AddComponent<LayoutElement>();layout.minWidth=layout.preferredWidth=320;PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        foreach(var row in content.Cast<Transform>()){var layout=row.GetComponent<LayoutElement>()??row.gameObject.AddComponent<LayoutElement>();layout.minWidth=layout.preferredWidth=320;}
        Place(Find("MapViewportFrame"),328,180,1568,610);Place(Find("MapLegend"),470,740,940,42);Place(Find("FitMap"),340,192,140,40);
        var drawer=Full(root,"PlanningDrawer");Place(drawer,1504,180,384,610);var dg=drawer.gameObject.AddComponent<CanvasGroup>();
        Move(Find("RouteEditor"),drawer);Place(Find("RouteEditor"),0,0,384,610);
        Place(Find("RouteTitle"),16,14,290,30);Place(Find("RouteSummary"),16,52,350,65);Place(Find("IndividualComparison"),16,122,350,80);Place(Find("NextRoadsTitle"),16,208,350,26);
        Place(Find("RoadViewport"),16,238,350,86);Place(Find("HistoryViewport"),16,330,350,36);Place(Find("UseSuggestedRoute"),16,410,350,42);
        Move(Find("Footer"),drawer);Place(Find("Footer"),0,458,384,152);Place(Find("Status"),12,4,360,48);Place(Find("DispatchRoute"),12,56,360,40);Place(Find("DispatchSuggestion"),12,104,360,40);
        Move(Find("RestartDemo"),root);Place(Find("RestartDemo"),1718,68,170,40);
        Move(Find("UndoRoad"),drawer);Move(Find("ClearRoute"),drawer);Place(Find("UndoRoad"),16,370,164,36);Place(Find("ClearRoute"),198,370,168,36);
        
        var deselect=M5SceneSetup.MakeButton(drawer,"Deselect","X",328,8,44,36);
        var slot=Find("BusinessPolicySlot");Place(slot,320,58,1280,100);
        var minus=M5SceneSetup.MakeButton(root,"PromiseMinus","-",1610,112,54,40);var plus=M5SceneSetup.MakeButton(root,"PromisePlus","+",1674,112,54,40);
        var ui=Manager.GetComponent<BusinessScreenUI>()??Undo.AddComponent<BusinessScreenUI>(Manager);
        Bind(ui,"router",Manager.GetComponent<ScreenRouter>());Bind(ui,"clock",Manager.GetComponent<GameClock>());Bind(ui,"orders",Manager.GetComponent<OrderListUI>());Bind(ui,"roster",Manager.GetComponent<DeliveryPersonSelector>());Bind(ui,"drawer",dg);Bind(ui,"mapFrame",Find("MapViewportFrame"));Bind(ui,"deselect",deselect);Bind(ui,"spawner",Manager.GetComponent<OrderSpawner>());Bind(ui,"minus",minus);Bind(ui,"plus",plus);
        var ob=Find("Selections").GetComponent<CanvasGroup>();if(ob==null)ob=Find("Selections").gameObject.AddComponent<CanvasGroup>();var cb=band.GetComponent<CanvasGroup>();if(cb==null)cb=band.gameObject.AddComponent<CanvasGroup>();Bind(ui,"ordersBand",ob);Bind(ui,"couriersBand",cb);UIVisibility.Set(drawer.gameObject,false);
        var stage=new SerializedObject(Manager.GetComponent<ScreenRouter>());stage.FindProperty("implementationStage").intValue=3;stage.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(Manager.scene);
    }
    public static void Settlement()
    {
        var panel=Find("DayResults");Full(panel.parent,"DayResults");
        var card=Find("ResultCard");Place(card,210,130,1500,820);Place(Find("LedgerViewport"),48,70,1404,590);Place(Find("ReplayDay"),1020,720,420,64);
        var advance=M5SceneSetup.MakeButton(card,"AdvancePage","Continue",1020,720,420,64);var skip=M5SceneSetup.MakeButton(card,"SkipPages","Skip",48,720,230,64);var all=M5SceneSetup.MakeButton(card,"ShowAll","Show all",302,720,270,64);
        var over=M5SceneSetup.MakeButton(card,"ToGameOver","Game over",1020,720,420,64);
        var root=Find("GameOverScreen");var backdrop=root.gameObject.AddComponent<Image>();backdrop.color=new Color(.06f,.10f,.16f,1);
        var summary=Text(root,"GameOverSummary","GAME OVER",128,140,1664,640,28);var retry=M5SceneSetup.MakeButton(root,"RetryCampaign","Start new",1020,880,330,72);var title=M5SceneSetup.MakeButton(root,"GameOverTitle","Title",1374,880,330,72);
        var ui=Manager.GetComponent<SettlementScreenUI>()??Undo.AddComponent<SettlementScreenUI>(Manager);
        Bind(ui,"router",Manager.GetComponent<ScreenRouter>());Bind(ui,"clock",Manager.GetComponent<GameClock>());Bind(ui,"campaign",Manager.GetComponent<CampaignManager>());Bind(ui,"economy",Manager.GetComponent<BusinessEconomy>());Bind(ui,"stats",Manager.GetComponent<DayStats>());Bind(ui,"roster",Manager.GetComponent<DeliveryPersonSelector>());Bind(ui,"flow",Manager.GetComponent<DeliveryFlowController>());Bind(ui,"score",Manager.GetComponent<ScoreManager>());Bind(ui,"reputation",Manager.GetComponent<ReputationManager>());Bind(ui,"progression",Manager.GetComponent<CourierProgression>());Bind(ui,"planner",Manager.GetComponent<DeliveryPlannerUI>());
        Bind(ui,"pageText",Find("ResultText").GetComponent<TextMeshProUGUI>());Bind(ui,"gameOverText",summary);Bind(ui,"advance",advance);Bind(ui,"skip",skip);Bind(ui,"all",all);Bind(ui,"replay",Find("ReplayDay").GetComponent<Button>());Bind(ui,"gameOver",over);Bind(ui,"retry",retry);Bind(ui,"title",title);
        var stage=new SerializedObject(Manager.GetComponent<ScreenRouter>());stage.FindProperty("implementationStage").intValue=4;stage.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(Manager.scene);
    }
    private static CanvasGroup Overlay(Transform parent,string name)
    {
        var r=Full(parent,name);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.04f,.07f,.12f,.98f);var group=r.gameObject.AddComponent<CanvasGroup>();UIVisibility.Set(r.gameObject,false);return group;
    }
    public static void TitleAndMenu()
    {
        var root=Find("TitleScreen");var choice=Find("SaveChoice");Full(choice.parent,"SaveChoice");choice.GetComponent<Image>().color=new Color(.04f,.07f,.12f,1);
        Text(choice,"GameName","PIZZA DELIVERY",360,150,1200,96,56);Place(Find("ChoiceText"),560,304,880,130);Place(Find("Continue"),720,500,480,72);Place(Find("NewGame"),720,588,480,72);
        var th=M5SceneSetup.MakeButton(choice,"TitleHelp","How to play",720,676,480,72);var quit=M5SceneSetup.MakeButton(choice,"Quit","Quit",720,764,480,72);
        var shared=Find("SharedHeader");Place(Find("CampaignDay"),1060,16,620,32);var hm=M5SceneSetup.MakeButton(shared,"HeaderMenu","Menu / Esc",1730,14,158,44);UIVisibility.Set(Find("PlannerScore").gameObject,false);
        var overlays=Full(Canvas.transform,"OverlayScreens");var menu=Overlay(overlays,"PauseMenu");var help=Overlay(overlays,"HelpOverlay");var confirm=Overlay(overlays,"ReturnTitleConfirmation");
        Text(menu.transform,"MenuTitle","MENU",620,230,680,76,36);var resume=M5SceneSetup.MakeButton(menu.transform,"Resume","Resume",720,360,480,72);var mh=M5SceneSetup.MakeButton(menu.transform,"MenuHelp","How to play",720,456,480,72);var mt=M5SceneSetup.MakeButton(menu.transform,"MenuTitleButton","Title",720,552,480,72);
        var ht=Text(help.transform,"HelpText","How to play",300,180,1320,560,28);var hp=M5SceneSetup.MakeButton(help.transform,"HelpPrevious","Previous",300,820,300,64);var hn=M5SceneSetup.MakeButton(help.transform,"HelpNext","Next",620,820,300,64);var hc=M5SceneSetup.MakeButton(help.transform,"HelpClose","Close",1320,820,300,64);
        Text(confirm.transform,"ConfirmText","Return to title?\nCurrent business progress will be lost.\nContinue resumes from the saved preparation.",400,300,1120,260,30);var yes=M5SceneSetup.MakeButton(confirm.transform,"ConfirmTitle","Return to title",800,650,380,72);var no=M5SceneSetup.MakeButton(confirm.transform,"CancelTitle","Cancel",1200,650,320,72);
        var ui=Manager.GetComponent<TitleMenuUI>()??Undo.AddComponent<TitleMenuUI>(Manager);
        Bind(ui,"router",Manager.GetComponent<ScreenRouter>());Bind(ui,"clock",Manager.GetComponent<GameClock>());Bind(ui,"save",Manager.GetComponent<CampaignSaveManager>());Bind(ui,"menu",menu);Bind(ui,"help",help);Bind(ui,"confirmation",confirm);Bind(ui,"choice",choice.gameObject);Bind(ui,"helpText",ht);Bind(ui,"titleStatus",Find("ChoiceText").GetComponent<TextMeshProUGUI>());
        Bind(ui,"resume",resume);Bind(ui,"menuHelp",mh);Bind(ui,"menuTitle",mt);Bind(ui,"confirmYes",yes);Bind(ui,"confirmNo",no);Bind(ui,"helpNext",hn);Bind(ui,"helpPrevious",hp);Bind(ui,"helpClose",hc);Bind(ui,"titleHelp",th);Bind(ui,"quit",quit);Bind(ui,"headerMenu",hm);Bind(ui,"continueGame",Find("Continue").GetComponent<Button>());Bind(ui,"newGame",Find("NewGame").GetComponent<Button>());
        var stage=new SerializedObject(Manager.GetComponent<ScreenRouter>());stage.FindProperty("implementationStage").intValue=5;stage.FindProperty("startAtTitle").boolValue=true;stage.ApplyModifiedProperties();
        UIVisibility.Set(root.gameObject,true);UIVisibility.Set(Find("HubScreen").gameObject,false);UIVisibility.Set(shared.gameObject,false);UIVisibility.Set(choice.gameObject,true);EditorSceneManager.MarkSceneDirty(Manager.scene);
    }
}
