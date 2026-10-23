/// <summary>One switch for presentation updates. Long editor calculations (simulations) set Suspended so views and
/// animations skip their per-frame work; game rules never read it.</summary>
public static class UiRendering
{
    public static bool Suspended {get;set;}
}
