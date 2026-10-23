using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>One-time, undoable scene migration. Copies existing serialized managers; never recreates their settings.</summary>
public static class GameSystemsOrganizer
{
    private static readonly Dictionary<Type,string> Groups=new()
    {
        {typeof(GameClock),"CoreSystems"},
        {typeof(DaySimulationSettings),"CoreSystems"},
        {typeof(ScreenRouter),"CoreSystems"},
        {typeof(GameMenu),"CoreSystems"},
        {typeof(MapVisuals),"CoreSystems"},
        {typeof(DeliveryFeedback),"CoreSystems"},
        {typeof(CampaignManager),"CampaignSystems"},
        {typeof(CampaignSaveManager),"CampaignSystems"},
        {typeof(BusinessEconomy),"CampaignSystems"},
        {typeof(DayStats),"CampaignSystems"},
        {typeof(ReputationManager),"CampaignSystems"},
        {typeof(ScoreManager),"CampaignSystems"},
        {typeof(RecruitmentManager),"CampaignSystems"},
        {typeof(CourierProgression),"CampaignSystems"},
        {typeof(TrainingManager),"CampaignSystems"},
        {typeof(FacilityManager),"CampaignSystems"},
        {typeof(CookingManager),"KitchenSystems"},
        {typeof(KitchenCrew),"KitchenSystems"},
        {typeof(HandoverManager),"KitchenSystems"},
        {typeof(OrderBook),"DeliverySystems"},
        {typeof(OrderSpawner),"DeliverySystems"},
        {typeof(CourierRoster),"DeliverySystems"},
        {typeof(DeliveryFlowController),"DeliverySystems"},
        {typeof(DeliveryPlanner),"DeliverySystems"},
        {typeof(DeliveryDynamics),"DeliverySystems"},
        {typeof(DeliveryMarkerView),"DeliverySystems"}
    };

    [MenuItem("Tools/Screens/Organize Game Systems")]
    public static void Menu()=>Debug.Log(Organize());

    /// <summary>Call in Edit Mode with Game open. Leaves the scene dirty for inspection and explicit saving.</summary>
    public static string Organize()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Exit Play Mode and wait for compilation first.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Game.unity")
            throw new InvalidOperationException("Open Assets/Scenes/Game.unity first. No scene was changed.");
        var roots=scene.GetRootGameObjects().Where(g=>g.name=="GameManager").ToArray();
        if(roots.Length!=1)throw new InvalidOperationException("Expected exactly one GameManager scene root.");
        var root=roots[0];
        if(PrefabUtility.IsPartOfPrefabInstance(root))
            throw new InvalidOperationException("GameManager is a prefab instance; migrate its prefab separately.");
        var original=root.GetComponents<MonoBehaviour>();
        if(original.Any(c=>c==null))throw new InvalidOperationException("GameManager has a missing script; repair it first.");
        var moving=original.Where(c=>Groups.ContainsKey(c.GetType())).ToArray();
        if(moving.Length==0)return "Game systems already organized; no changes.";
        // RequireComponent can silently add dependencies. None of the audited manager types currently has it.
        foreach(var component in moving)
        {
            if(component.GetType().GetCustomAttributes(typeof(RequireComponent),true).Length>0)
                throw new InvalidOperationException(component.GetType().Name+" now requires another component; review migration first.");
            var group=root.transform.Find(Groups[component.GetType()]);
            if(group!=null && group.GetComponent(component.GetType())!=null)
                throw new InvalidOperationException("Both root and group contain "+component.GetType().Name+"; resolve the duplicate first.");
        }
        foreach(string name in Groups.Values.Distinct())
        {
            var group=root.transform.Find(name);
            if(group!=null && (group.localPosition!=Vector3.zero || group.localRotation!=Quaternion.identity || group.localScale!=Vector3.one))
                throw new InvalidOperationException(name+" must have an identity transform before migration.");
        }
        int beforeMissing=MissingReferences(scene);
        var beforeInventory=Inventory(scene);
        Undo.IncrementCurrentGroup();
        int undoGroup=Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Organize GameManager systems");
        try
        {
            var replacements=new Dictionary<Object,Object>();
            foreach(var source in moving)
            {
                string name=Groups[source.GetType()];
                var group=root.transform.Find(name);
                if(group==null)
                {
                    var child=new GameObject(name){layer=root.layer};
                    Undo.RegisterCreatedObjectUndo(child,"Create "+name);
                    Undo.SetTransformParent(child.transform,root.transform,"Parent "+name);
                    child.transform.localPosition=Vector3.zero;
                    child.transform.localRotation=Quaternion.identity;
                    child.transform.localScale=Vector3.one;
                    group=child.transform;
                }
                var copy=Undo.AddComponent(group.gameObject,source.GetType());
                EditorUtility.CopySerialized(source,copy);
                replacements.Add(source,copy);
            }
            int remapped=0;
            foreach(var component in Components(scene))
            {
                var serialized=new SerializedObject(component);
                var property=serialized.GetIterator();
                bool changed=false;
                while(property.Next(true))
                {
                    if(property.propertyType!=SerializedPropertyType.ObjectReference)continue;
                    var value=property.objectReferenceValue;
                    if(value==null || !replacements.TryGetValue(value,out var replacement))continue;
                    if(!changed)Undo.RecordObject(component,"Remap manager references");
                    property.objectReferenceValue=replacement;
                    changed=true;
                    remapped++;
                }
                if(changed)serialized.ApplyModifiedProperties();
            }
            // Fail before deletion if any field still points at an original manager.
            foreach(var component in Components(scene))
            {
                var serialized=new SerializedObject(component);
                var property=serialized.GetIterator();
                while(property.Next(true))
                    if(property.propertyType==SerializedPropertyType.ObjectReference && property.objectReferenceValue!=null
                        && replacements.ContainsKey(property.objectReferenceValue))
                        throw new InvalidOperationException("Unmigrated reference: "+component.GetType().Name+"."+property.propertyPath);
            }
            // Relative component order within each group follows the original root order.
            foreach(var source in moving.Reverse())Undo.DestroyObjectImmediate(source);
            if(!beforeInventory.SequenceEqual(Inventory(scene)))
                throw new InvalidOperationException("Component inventory changed unexpectedly.");
            int afterMissing=MissingReferences(scene);
            if(afterMissing!=beforeMissing)
                throw new InvalidOperationException($"Missing references changed: {beforeMissing} -> {afterMissing}.");
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            return $"Organized {moving.Length} managers into four identity-transform groups; remapped {remapped} references. "
                +$"Missing references: {beforeMissing} -> {afterMissing}. Scene left unsaved; Undo available.";
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            throw;
        }
    }

    private static Component[] Components(Scene scene)=>scene.GetRootGameObjects()
        .SelectMany(root=>root.GetComponentsInChildren<Component>(true)).Where(c=>c!=null).ToArray();

    private static string[] Inventory(Scene scene)=>Components(scene).OfType<MonoBehaviour>()
        .Select(c=>c.GetType().FullName).OrderBy(name=>name,StringComparer.Ordinal).ToArray();

    private static int MissingReferences(Scene scene)
    {
        int count=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true))
            .Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        foreach(var component in Components(scene))
        {
            var serialized=new SerializedObject(component);
            var property=serialized.GetIterator();
            while(property.Next(true))
                if(property.propertyType==SerializedPropertyType.ObjectReference && property.objectReferenceValue==null
                    && property.objectReferenceInstanceIDValue!=0)count++;
        }
        return count;
    }
}
