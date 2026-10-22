using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Shop scene (店内シーン) setup through Editor APIs. Each step is idempotent and saves only through the scene.</summary>
public static class ShopSceneSetup
{
    private static GameObject Manager=>GameObject.Find("GameManager");
    private static T Ensure<T>(GameObject go) where T:Component{var c=go.GetComponent<T>();if(c==null)c=Undo.AddComponent<T>(go);return c;}
    private static void Bind(UnityEngine.Object target,string property,UnityEngine.Object value)
    {
        var so=new SerializedObject(target);var p=so.FindProperty(property);
        if(p==null)throw new InvalidOperationException(target.GetType().Name+" has no field "+property);
        p.objectReferenceValue=value;so.ApplyModifiedProperties();
    }
    /// <summary>S2 logic: kitchen staff roster, handover bags and the save format.</summary>
    public static void Logic()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var go=Manager;if(go==null)throw new InvalidOperationException("GameManager not found.");
        var crew=Ensure<KitchenCrew>(go);var handover=Ensure<HandoverManager>(go);
        Bind(handover,"orders",go.GetComponent<OrderListUI>());Bind(handover,"roster",go.GetComponent<DeliveryPersonSelector>());Bind(handover,"flow",go.GetComponent<DeliveryFlowController>());Bind(handover,"clock",go.GetComponent<GameClock>());
        var cooking=go.GetComponent<CookingManager>();Bind(cooking,"crew",crew);Bind(cooking,"handover",handover);
        Bind(go.GetComponent<BusinessEconomy>(),"crew",crew);Bind(go.GetComponent<RecruitmentManager>(),"crew",crew);
        var campaign=go.GetComponent<CampaignManager>();Bind(campaign,"crew",crew);Bind(campaign,"cooking",cooking);Bind(campaign,"handover",handover);
        var save=go.GetComponent<CampaignSaveManager>();Bind(save,"crew",crew);
        var so=new SerializedObject(save);so.FindProperty("formatVersion").intValue=3;so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(go.scene);
    }
}
