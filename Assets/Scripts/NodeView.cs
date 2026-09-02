using UnityEngine;

public class NodeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    public void Initialize(MapNode node)
    {
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
    }
}