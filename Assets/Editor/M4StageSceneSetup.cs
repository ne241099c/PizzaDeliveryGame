using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Incremental authoring: keeps the existing Canvas and route editor.</summary>
public static class M4StageSceneSetup
{
    public static void AddPromiseUI()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play first.");
        var canvas=Object.FindFirstObjectByType<Canvas>();
        if(canvas.transform.Find("DeliveryPolicy")!=null)return;
        Undo.IncrementCurrentGroup();
        foreach(RectTransform child in canvas.transform)
        {
            Undo.RecordObject(child,"Make room for delivery policy");
            if(child.anchoredPosition.y<=-100)child.anchoredPosition+=Vector2.down*114;
            if(child.name=="TopBackdrop")child.sizeDelta=new Vector2(1280,323);
            if(child.name=="DayResults")child.sizeDelta=new Vector2(1280,840);
        }
        var scaler=canvas.GetComponent<CanvasScaler>();Undo.RecordObject(scaler,"Policy resolution");
        scaler.referenceResolution=new Vector2(1280,840);
        Undo.RecordObject(Camera.main,"Policy map viewport");Camera.main.rect=new Rect(0.25f,0.23f,0.31f,0.38f);
        var panel=Rect(canvas.transform,"DeliveryPolicy",20,90,1240,112);
        panel.gameObject.AddComponent<Image>().color=new Color(0.12f,0.17f,0.25f);
        var label=Text(panel,"PromiseText","Delivery promise: 60.0s",16,7,420,26,19);
        var demand=Text(panel,"DemandText","Demand x1.00 / about 25.0s per order",460,10,760,32,20);
        var reference=Text(panel,"ArrivalReference","Fastest arrival (reference)",460,55,760,43,18);
        var sliderRect=Rect(panel,"PromiseSlider",22,43,404,21);
        var slider=sliderRect.gameObject.AddComponent<Slider>();slider.minValue=1;slider.maxValue=240;slider.value=60;
        var background=Rect(sliderRect,"Background",0,6,404,9).gameObject.AddComponent<Image>();background.color=Color.gray;
        var handleArea=Rect(sliderRect,"HandleArea",0,0,404,21);
        var handle=Rect(handleArea,"Handle",0,0,18,23).gameObject.AddComponent<Image>();handle.color=new Color(0.32f,0.88f,0.69f);
        slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.direction=Slider.Direction.LeftToRight;
        var manager=GameObject.Find("GameManager");
        var ui=manager.GetComponent<DeliveryPromiseUI>()??Undo.AddComponent<DeliveryPromiseUI>(manager);
        Bind(ui,"spawner",manager.GetComponent<OrderSpawner>(),"map",Object.FindFirstObjectByType<MapManager>(),
            "clock",manager.GetComponent<GameClock>(),"promiseSlider",slider,"promiseText",label,"demandText",demand,"referenceText",reference);
        Canvas.ForceUpdateCanvases();EditorSceneManager.MarkSceneDirty(manager.scene);
    }

    public static void AddReputationUI()
    {
        var manager=GameObject.Find("GameManager");
        var reputation=manager.GetComponent<ReputationManager>()??Undo.AddComponent<ReputationManager>(manager);
        var panel=Object.FindFirstObjectByType<Canvas>().transform.Find("DeliveryPolicy");
        var existing=panel.Find("ReputationText");
        var label=existing!=null?existing.GetComponent<TextMeshProUGUI>():Text(panel,"ReputationText","Reputation: 50.0 / Orders x1.00",16,74,430,28,18);
        Bind(manager.GetComponent<DeliveryPromiseUI>(),"reputation",reputation,"reputationText",label);
        Bind(manager.GetComponent<OrderSpawner>(),"reputation",reputation);
        Bind(manager.GetComponent<DeliveryFlowController>(),"reputation",reputation);
        EditorSceneManager.MarkSceneDirty(manager.scene);
    }

    public static void AddDynamics()
    {
        var manager=GameObject.Find("GameManager");
        var rules=manager.GetComponent<DeliveryDynamics>()??Undo.AddComponent<DeliveryDynamics>(manager);
        Bind(rules,"roster",manager.GetComponent<DeliveryPersonSelector>());
        Bind(manager.GetComponent<DeliveryFlowController>(),"dynamics",rules);
        EditorSceneManager.MarkSceneDirty(manager.scene);
    }

    public static void AddRosterScroll()
    {
        var selector=Object.FindFirstObjectByType<DeliveryPersonSelector>();
        var content=(RectTransform)new SerializedObject(selector).FindProperty("deliveryPersonListContent").objectReferenceValue;
        if(content.parent.name=="CourierViewport")return;
        var viewport=Rect(content.parent,"CourierViewport",content.anchoredPosition.x,-content.anchoredPosition.y,content.sizeDelta.x,content.sizeDelta.y);
        viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,0.01f);
        viewport.gameObject.AddComponent<RectMask2D>();
        Undo.SetTransformParent(content,viewport,"Keep roster inside scroll viewport");Undo.RecordObject(content,"Roster scroll content");
        content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(0,1);
        content.anchoredPosition=Vector2.zero;content.sizeDelta=Vector2.zero;
        var fitter=content.GetComponent<ContentSizeFitter>()??Undo.AddComponent<ContentSizeFitter>(content.gameObject);
        fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=Undo.AddComponent<ScrollRect>(viewport.gameObject);scroll.content=content;scroll.viewport=viewport;
        scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=28;
        foreach(var button in content.GetComponentsInChildren<Button>(true))
        {
            var layout=button.GetComponent<LayoutElement>();Undo.RecordObject(layout,"Readable courier row");layout.minHeight=layout.preferredHeight=88;
            var label=button.GetComponentInChildren<TextMeshProUGUI>();Undo.RecordObject(label.rectTransform,"Courier text padding");
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
            label.rectTransform.offsetMin=new Vector2(8,8);label.rectTransform.offsetMax=new Vector2(-8,-4);
            PrefabUtility.RecordPrefabInstancePropertyModifications(label.rectTransform);
            if(button.transform.Find("StaminaBar")==null)
            {
                var bar=Rect(button.transform,"StaminaBar",12,80,226,4);bar.gameObject.AddComponent<Image>().color=new Color(0.2f,0.25f,0.3f);
                bar.GetComponent<Image>().raycastTarget=false;
                var fill=Rect(bar,"Fill",0,0,226,4);fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;
                fill.gameObject.AddComponent<Image>().color=new Color(0.2f,0.75f,0.45f);fill.GetComponent<Image>().raycastTarget=false;
            }
        }
        var title=GameObject.Find("CouriersTitle").GetComponent<TextMeshProUGUI>();Undo.RecordObject(title,"Roster scroll hint");title.text="2 / COURIERS (scroll)";
        Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;
        EditorSceneManager.MarkSceneDirty(selector.gameObject.scene);
    }

    public static void CompleteResults()
    {
        var manager=GameObject.Find("GameManager");
        ConfigureViewport();
        Bind(manager.GetComponent<DeliveryPlannerUI>(),"reputation",manager.GetComponent<ReputationManager>());
        var card=Object.FindFirstObjectByType<Canvas>().transform.Find("DayResults/ResultCard").GetComponent<RectTransform>();
        Undo.RecordObject(card.parent,"Results in front of policy controls");card.parent.SetAsLastSibling();
        Undo.RecordObject(card,"Results room for reputation and roster");card.anchoredPosition=new Vector2(340,-110);card.sizeDelta=new Vector2(600,620);
        var label=card.Find("ResultText").GetComponent<TextMeshProUGUI>();Undo.RecordObject(label,"Results font");Undo.RecordObject(label.rectTransform,"Results size");
        label.fontSize=24;label.rectTransform.anchoredPosition=new Vector2(36,-24);label.rectTransform.sizeDelta=new Vector2(528,470);
        label.text="DAY COMPLETE\n\nScore: 0\nDelivered: 0\nLate: 0\nCancelled: 0\nReputation: 50.0\n\nCouriers / Load / Stamina";
        var replay=card.Find("ReplayDay").GetComponent<RectTransform>();Undo.RecordObject(replay,"Results replay position");replay.anchoredPosition=new Vector2(140,-535);
        EditorSceneManager.MarkSceneDirty(manager.scene);
    }

    public static void ConfigureViewport()
    {
        var canvas=Object.FindFirstObjectByType<Canvas>();
        var scaler=canvas.GetComponent<CanvasScaler>();Undo.RecordObject(scaler,"Fit complete layout");
        scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var existing=canvas.transform.Find("MapViewportFrame");
        var frame=existing!=null?existing.GetComponent<RectTransform>():Rect(canvas.transform,"MapViewportFrame",320,318,397,319);
        var view=frame.GetComponent<MapViewportUI>()??Undo.AddComponent<MapViewportUI>(frame.gameObject);
        Bind(view,"mapCamera",Camera.main,"viewport",frame);
        Undo.RecordObject(Camera.main,"Fit map viewport to UI slot");
        Canvas.ForceUpdateCanvases();view.ApplyViewport();
        canvas.transform.Find("DayResults").SetAsLastSibling();
    }

    public static void Bind(Object target,params object[] pairs)
    {
        Undo.RecordObject(target,"Assign M4 scene references");var so=new SerializedObject(target);
        for(int i=0;i<pairs.Length;i+=2)so.FindProperty((string)pairs[i]).objectReferenceValue=pairs[i+1] as Object;
        so.ApplyModifiedProperties();
    }
    public static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
    {
        var go=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(go,"Add M4 control");
        go.layer=5;go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();
        rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
    }
    public static TextMeshProUGUI Text(Transform parent,string name,string value,float x,float y,float w,float h,float size)
    {
        var rect=Rect(parent,name,x,y,w,h);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font=TMP_Settings.defaultFontAsset;label.text=value;label.fontSize=size;
        label.alignment=TextAlignmentOptions.MidlineLeft;label.raycastTarget=false;
        return label;
    }
}
