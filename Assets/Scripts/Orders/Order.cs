using System;

public enum BakingState { Waiting, Cooking, Ready }

public enum OrderStatus { Pending, Dispatched, Delivered, Cancelled }

/// <summary>Toppings on the ingredient shelf. Seafood requires shrimp and squid.</summary>
public enum Topping { Basil, Pepperoni, Shrimp, Squid }

public enum PizzaKind { Margherita, Pepperoni, Seafood }

[Serializable]
public class Order
{
    public string Id;
    public string DestinationNodeId;
    public string DestinationName;
    public float SpawnedAt;
    public float Deadline;
    public OrderStatus Status { get; internal set; }
    /// <summary>Waiting = ticket not started, Cooking = on a bench/oven/shelf/open box, Ready = packed in a closed box.</summary>
    public BakingState Baking {get;internal set;}
    /// <summary>Quality stars 1-3 decided when the box is closed; 0 until then. Pre-packed orders count as one star.</summary>
    public int Stars {get;internal set;}
    public PizzaKind Kind;
    public Topping[] RequiredToppings=>Kind==PizzaKind.Seafood?new[]{Topping.Shrimp,Topping.Squid}:new[]{Kind==PizzaKind.Pepperoni?Topping.Pepperoni:Topping.Basil};
    public Topping RequiredTopping=>RequiredToppings[0];
    public string PizzaName=>Kind==PizzaKind.Seafood?"シーフード":Kind==PizzaKind.Pepperoni?"ペパロニ":"マルゲリータ";
    public string Ingredients=>string.Join("・",System.Array.ConvertAll(RequiredToppings,ShopStrings.Topping));
    public bool IsReady => Baking==BakingState.Ready;
    // Compatibility for M3 callers; all timestamps now use GameClock's seconds.
    public float OrderedAt => SpawnedAt;
    public float TimeLimit => Deadline - SpawnedAt;

    public Order(string destinationNodeId, string destinationName, float orderedAt, float timeLimit, BakingState baking=BakingState.Waiting, PizzaKind kind=PizzaKind.Margherita)
    {
        Kind=kind;
        Id = Guid.NewGuid().ToString();
        DestinationNodeId = destinationNodeId;
        DestinationName = destinationName;
        SpawnedAt = orderedAt;
        Deadline = orderedAt + timeLimit;
        Status = OrderStatus.Pending;
        Baking=baking;
        if(baking==BakingState.Ready)Stars=1;
    }
}
