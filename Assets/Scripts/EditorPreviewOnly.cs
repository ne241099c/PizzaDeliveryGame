using UnityEngine;

/// <summary>Static scene preview for the sample map. Runtime geometry is owned by MapManager.</summary>
public sealed class EditorPreviewOnly : MonoBehaviour
{
    private void Awake() => gameObject.SetActive(false);
}
