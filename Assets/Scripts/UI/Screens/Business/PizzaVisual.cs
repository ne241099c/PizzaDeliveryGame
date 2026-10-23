using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A pizza drawn in layers (dough, sauce, cheese, topping, burn).</summary>
public sealed class PizzaVisual
{
    public RectTransform Root;private Image dough,sauce,cheese,topping,secondTopping,burnt;public RectTransform Blobs;
    public static PizzaVisual Make(Transform parent,string name,float x,float y,float size)
    {
        var v=new PizzaVisual{Root=UiKit.Box(parent,name,x,y,size,size)};
        v.dough=Layer(v.Root,"Dough","pizza_base");
        var mask=UiKit.Stretch(v.Root,"SauceMask",size*.12f,size*.12f,size*.12f,size*.12f);
        var m=mask.gameObject.AddComponent<Image>();
        m.sprite=UiKit.Circle;
        m.raycastTarget=false;
        mask.gameObject.AddComponent<Mask>().showMaskGraphic=false;v.Blobs=mask;
        v.sauce=Layer(v.Root,"Sauce","pizza_sauce");
        v.cheese=Layer(v.Root,"Cheese","pizza_cheese");
        v.topping=Layer(v.Root,"Topping","pizza_basil");
        v.secondTopping=Layer(v.Root,"Squid","pizza_squid");
        v.secondTopping.rectTransform.localScale=Vector3.one*UiKit.T.squidToppingScale;
        v.burnt=Layer(v.Root,"Burnt","pizza_burnt");
        return v;
    }
    private static Image Layer(RectTransform root,string name,string sprite)
    {
        var i=UiKit.Stretch(root,name).gameObject.AddComponent<Image>();
        i.sprite=UiKit.Art(sprite);
        i.raycastTarget=false;
        i.preserveAspect=true;
        return i;
    }
    /// <summary>Shows a job. For staff assembly the layers appear over time (fraction 0-1).</summary>
    public void Show(PizzaJob job,float staffFraction=1,bool paintingPreview=true)
    {
        Root.gameObject.SetActive(job!=null);if(job==null)return;
        var stage=job.Stage;bool staffAssembling=job.Worker==PizzaWorker.Staff && stage==PizzaStage.Topping && staffFraction<1;
        int visible=staffAssembling?(staffFraction<.25f?1:staffFraction<.55f?2:4):stage switch{PizzaStage.Ticket=>0,PizzaStage.Dough=>1,PizzaStage.Sauce=>2,PizzaStage.Cheese=>3,_=>4};
        dough.enabled=visible>=1;
        bool hand=paintingPreview && job.Worker==PizzaWorker.Player && stage<=PizzaStage.Topping;
        sauce.enabled=visible>=2 && !hand;Blobs.gameObject.SetActive(hand && visible>=1);
        cheese.enabled=visible>=3;
        topping.enabled=job.AddedToppings.Count>0 || visible>=4 && job.AddedTopping.HasValue || staffAssembling && visible>=4;
        var t=job.AddedTopping??job.Order.RequiredTopping;
        topping.sprite=UiKit.Art(t==Topping.Pepperoni?"pizza_pepperoni":t==Topping.Shrimp?"pizza_shrimp":"pizza_basil");
        secondTopping.enabled=visible>=4 && job.AddedToppings.Contains(Topping.Squid);
        bool baked=stage>=PizzaStage.Baked;var warm=new Color(.93f,.80f,.62f);dough.color=baked?warm:Color.white;
        var cookedTint=job.Burnt?Color.Lerp(Color.white,UiKit.T.line,.8f):Color.white;
        sauce.color=cheese.color=topping.color=secondTopping.color=cookedTint;
        if(job.Burnt)dough.color=Color.Lerp(warm,UiKit.T.line,.8f);
        burnt.enabled=job.Burnt;
    }
    public void ClearBlobs(){for(int i=Blobs.childCount-1;i>=0;i--)Object.Destroy(Blobs.GetChild(i).gameObject);}
}

