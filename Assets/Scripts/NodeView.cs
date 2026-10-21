using UnityEngine;
using TMPro;

public class NodeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    private TextMeshPro nodeLabel;
    private string nodeId;
    private Color baseColor;
    private Vector3 baseScale;

    public void Initialize(MapNode node)
    {
        nodeId = node.Id;
        gameObject.name = "Node_" + node.Id;
        transform.position = node.Position;

        if (node.IsStore)
        {
            spriteRenderer.color = Color.red;
            transform.localScale = Vector3.one * 0.45f;
        }
        else if (node.NodeType == NodeType.DeliveryAddress)
        {
            spriteRenderer.color = Color.magenta;
            transform.localScale = Vector3.one * 0.35f;
        }
        else
        {
            spriteRenderer.color = Color.white;
            transform.localScale = Vector3.one * 0.3f;
        }

        baseColor = spriteRenderer.color;
        baseScale = transform.localScale;
        var labelObject = new GameObject("NodeLabel");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localScale = Vector3.one / transform.localScale.x;
        labelObject.transform.position = transform.position + Vector3.down * 0.55f;
        var label = labelObject.AddComponent<TextMeshPro>();
        nodeLabel = label;
        label.font = TMP_Settings.defaultFontAsset;
        label.text = node.Id;
        label.fontSize = 4.5f;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(2f, 0.7f);
        label.GetComponent<MeshRenderer>().sortingOrder = 10;
    }

    public void SetCurrent(bool current)
    {
        spriteRenderer.color = current ? new Color(0.24f, 0.78f, 1f) : baseColor;
        transform.localScale = current ? baseScale * 1.25f : baseScale;
        nodeLabel.text = nodeId + (current ? " *" : "");
        nodeLabel.color = current ? new Color(0.24f, 0.78f, 1f) : Color.white;
    }

}
