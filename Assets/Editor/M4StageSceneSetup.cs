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
