using UnityEngine;
using TMPro;

public class NodeView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    private TextMeshPro nodeLabel;
    private string nodeId;
    private string caption;
    private Color baseColor;
    private Vector3 baseScale;

    public void Initialize(MapNode node)=>Initialize(node,node.Position);
    /// <summary>Draws the node at drawAt; a delivery home drawn beside its road gets a driveway to its frontage.</summary>
    public void Initialize(MapNode node,Vector2 drawAt)
    {
        nodeId = node.Id;
        gameObject.name = "Node_" + node.Id;
        transform.position = drawAt;
        if((drawAt-node.Position).sqrMagnitude>1e-6f)
        {
            var drive=new GameObject("Driveway").AddComponent<LineRenderer>();
            drive.transform.SetParent(transform,false);
            drive.sharedMaterial=spriteRenderer.sharedMaterial;
            drive.useWorldSpace=true;drive.positionCount=2;
            drive.SetPosition(0,new Vector3(node.Position.x,node.Position.y,0));drive.SetPosition(1,new Vector3(drawAt.x,drawAt.y,0));
            drive.startWidth=drive.endWidth=UITheme.Active.backstreetWidth*.7f;
            drive.startColor=drive.endColor=UITheme.Active.backRoadEdge;
            drive.sortingOrder=4;
        }

        var theme=UITheme.Active;
        caption=node.IsStore?"ピザ屋":node.NodeType==NodeType.DeliveryAddress?"配達先 "+node.Id.Replace("Goal",""):node.Id;
        spriteRenderer.sortingOrder=5;
        spriteRenderer.sprite=MapVisuals.Shape(node.IsStore || node.NodeType==NodeType.Intersection);
        spriteRenderer.color=node.IsStore?theme.accent:node.NodeType==NodeType.DeliveryAddress?theme.block:theme.muted;
        transform.localScale=Vector3.one*(node.NodeType==NodeType.Intersection && !node.IsStore?theme.nodeSize*.5f:theme.nodeSize);
        if(node.IsStore || node.NodeType==NodeType.DeliveryAddress)
        {
            var icon=new GameObject(node.IsStore?"PizzaSlice":"HouseRoof");
            icon.transform.SetParent(transform,false);
            var line=icon.AddComponent<LineRenderer>();
            line.sharedMaterial=spriteRenderer.sharedMaterial;
            line.useWorldSpace=false;
            line.positionCount=3;
            line.SetPosition(0,new Vector3(-.45f,.15f,0));
            line.SetPosition(1,new Vector3(0,.65f,0));
            line.SetPosition(2,new Vector3(.45f,.15f,0));
            line.startWidth=line.endWidth=.12f;
            line.startColor=line.endColor=node.IsStore?theme.money:theme.ink;
            line.sortingOrder=6;
        }
        // The shop has a UI badge. Homes remain visible as buildings even when they have no order pin.
        if(node.IsStore)
        {
            spriteRenderer.enabled=false;
            foreach(var line in GetComponentsInChildren<LineRenderer>())line.enabled=false;
        }
        else if(node.NodeType==NodeType.Intersection){spriteRenderer.color=theme.backRoadEdge;transform.localScale=Vector3.one*theme.nodeSize*.45f;}
        // Delivery homes stand out from the decorative roadside buildings.
        else if(node.NodeType==NodeType.DeliveryAddress){spriteRenderer.color=theme.mapHomeFill;transform.localScale=Vector3.one*theme.nodeSize*theme.mapHomeScale;}
        baseColor = spriteRenderer.color;
        baseScale = transform.localScale;
        var labelObject = new GameObject("NodeLabel");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localScale = Vector3.one / transform.localScale.x;
        labelObject.transform.position = transform.position + Vector3.down * 0.55f;
        var label = labelObject.AddComponent<TextMeshPro>();
        nodeLabel = label;
        label.font = UITheme.Active.font;
        label.text = caption;
        label.fontSize = UITheme.Active.worldLabelSize;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(2f, 0.7f);
        label.GetComponent<MeshRenderer>().sortingOrder = 10;
        label.enabled = false;
    }

    public void SetCurrent(bool current)
    {
        spriteRenderer.color = current ? UITheme.Active.accent : baseColor;
        transform.localScale = current ? baseScale * 1.25f : baseScale;
        nodeLabel.text = caption + (current ? " *" : "");
        nodeLabel.color = current ? UITheme.Active.accent : UITheme.Active.ink;
        spriteRenderer.color = current ? UITheme.Active.tomato : baseColor;
    }

}
