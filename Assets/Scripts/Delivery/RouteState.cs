using System.Collections.Generic;

public class RouteState
{
    public List<string> ActiveEdgeIds = new();
    public float TotalDistance;
    public float EstimatedTime;
}