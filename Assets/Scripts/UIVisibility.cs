using System.Linq;
using UnityEngine;

/// <summary>Presentation visibility without disabling components or changing simulation state.</summary>
public static class UIVisibility
{
    public static void Set(GameObject target, bool visible)
    {
        if (target == null) return;
        var group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.AddComponent<CanvasGroup>();
        group.alpha = visible ? 1 : 0;
        group.interactable = group.blocksRaycasts = visible;
    }
    public static Transform Find(Transform root, string name)
        => root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
}
