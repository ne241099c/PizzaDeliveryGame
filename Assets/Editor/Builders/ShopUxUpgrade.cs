using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Updates only the affected prefab parts. Existing artwork, bindings and manual layout edits stay in place.</summary>
public static class ShopUxUpgrade
{
    private static RectTransform Part(SerializedObject owner,string field)=>(RectTransform)owner.FindProperty(field).objectReferenceValue;
    public static string ApplyPlanningUpgrade()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play first.");
        const string path="Assets/Prefabs/UI/BusinessView.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var so=new SerializedObject(root.GetComponent<BusinessView>());
            var bubble=Part(so,"bubble");UiKit.Place(bubble,840,196,944,48);
            var here=bubble.Find("Here") as RectTransform;if(here!=null)UiKit.Place(here,12,10,96,28);
            var text=(TMPro.TextMeshProUGUI)so.FindProperty("bubbleText").objectReferenceValue;
            UiKit.Place(text.rectTransform,120,4,812,40);
            text.fontSize=UITheme.Active.captionSize;text.enableAutoSizing=true;text.fontSizeMin=UITheme.Active.captionSize;
            var content=Part(so,"dispatchOrderContent");var hint=content.parent.parent.Find("Hint").GetComponent<TMPro.TextMeshProUGUI>();
            hint.text=UiStrings.ReserveShortHint;hint.enableAutoSizing=true;hint.textWrappingMode=TMPro.TextWrappingModes.NoWrap;hint.overflowMode=TMPro.TextOverflowModes.Ellipsis;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var clock=Object.FindFirstObjectByType<GameClock>();
        if(clock==null)throw new InvalidOperationException("Open Game scene first.");
        var c=new SerializedObject(clock);c.FindProperty("planningTimeRate").floatValue=.1f;c.ApplyModifiedProperties();
        var roster=Object.FindFirstObjectByType<CourierRoster>();var r=new SerializedObject(roster);
        var people=r.FindProperty("deliveryPeople");
        for(int i=0;i<people.arraySize;i++)
        {
            var person=people.GetArrayElementAtIndex(i);person.FindPropertyRelative("capacityPotential").intValue=DeliveryPerson.MaximumCapacity;
            if(person.FindPropertyRelative("Name").stringValue=="Aoi")person.FindPropertyRelative("capacity").intValue=DeliveryPerson.MaximumCapacity;
        }
        r.ApplyModifiedProperties();
        var progression=new SerializedObject(Object.FindFirstObjectByType<CourierProgression>());
        progression.FindProperty("earnedExperienceMultiplier").floatValue=3;progression.ApplyModifiedProperties();
        var facility=new SerializedObject(Object.FindFirstObjectByType<FacilityManager>());
        facility.FindProperty("maximumEquipmentLevel").intValue=10;facility.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(clock.gameObject.scene);
        EditorSceneManager.SaveScene(clock.gameObject.scene);
        return "Applied planning clock, initial Aoi capacity, reservation guide and compact guide prefab.";
    }
    public static string Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play before applying prefab changes.");
        const string path="Assets/Prefabs/UI/BusinessView.prefab";
        string backup="Library/PrefabBackups/ShopUx-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);File.Copy(path,backup+"/BusinessView.prefab",true);
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view=root.GetComponent<BusinessView>();var so=new SerializedObject(view);
            if(Part(so,"ovenQueueContent")==null)view.BuildOvenQueue();
            so.Update();
            var lanes=Part(so,"laneRow");var viewport=(RectTransform)lanes.parent;
            UiKit.Place(viewport,32,764,1856,296);lanes.sizeDelta=new Vector2(lanes.sizeDelta.x,296);
            ScrollNavigation.Configure(viewport.GetComponent<ScrollRect>(),true);
            var content=Part(so,"dispatchOrderContent");var orderViewport=(RectTransform)content.parent;
            orderViewport.sizeDelta=new Vector2(328,428);content.sizeDelta=new Vector2(328,428);
            var panel=(RectTransform)orderViewport.parent;panel.sizeDelta=new Vector2(344,540);
            var hint=panel.Find("Hint") as RectTransform;if(hint!=null)UiKit.Place(hint,18,496,308,32);
            var dispatch=Part(so,"dispatch");var legend=dispatch.Find("RouteLegend") as RectTransform;
            if(legend!=null)UiKit.Place(legend,430,724,1120,32);
            var minimap=dispatch.Find("Minimap") as RectTransform;if(minimap!=null)UiKit.Place(minimap,1640,584,248,168);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var theme=UITheme.Active;theme.cityLabelMinimumBlockPixels=300;
        EditorUtility.SetDirty(theme);AssetDatabase.SaveAssetIfDirty(theme);
        var map=UnityEngine.Object.FindFirstObjectByType<MapManager>(FindObjectsInactive.Include);
        if(map==null)throw new InvalidOperationException("Open Game scene to update its Inspector settings.");
        var settings=new SerializedObject(map);
        settings.FindProperty("deliverySpeedMultiplier").floatValue=1.6f;
        settings.FindProperty("backstreetVisualOffset").floatValue=.85f;
        settings.FindProperty("citySettings.housesPerBlock").intValue=4;
        settings.FindProperty("citySettings.maximumDestinations").intValue=64;
        var templates=settings.FindProperty("citySettings.templates");
        for(int i=0;i<templates.arraySize;i++)
        {
            var positions=templates.GetArrayElementAtIndex(i).FindPropertyRelative("additionalAddressPositions");
            if(positions.arraySize==2)continue;
            positions.arraySize=2;positions.GetArrayElementAtIndex(0).vector2Value=new Vector2(.25f,-.3f);positions.GetArrayElementAtIndex(1).vector2Value=new Vector2(.25f,-.7f);
        }
        settings.ApplyModifiedProperties();
        var kitchen=UnityEngine.Object.FindFirstObjectByType<CookingManager>(FindObjectsInactive.Include);
        var cooking=new SerializedObject(kitchen);cooking.FindProperty("wasteCost").floatValue=0;cooking.ApplyModifiedProperties();
        var marker=UnityEngine.Object.FindFirstObjectByType<DeliveryMarkerView>(FindObjectsInactive.Include);
        var markerSettings=new SerializedObject(marker);markerSettings.FindProperty("minimumMarkerPixels").floatValue=44;markerSettings.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(map.gameObject.scene);
        return "Updated BusinessView prefab parts and Game Inspector settings. Original prefab: "+backup;
    }
}

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

// Scene migration is invoked explicitly; it never runs automatically on Play.
