using UnityEngine;

/// <summary>Marks a subtree that styles itself (prefab-built screens). UIThemeDriver, which restyles the older
/// scene-authored screens every frame, leaves everything under this marker alone.</summary>
[DisallowMultipleComponent]
public sealed class ThemeOptOut : MonoBehaviour {}
