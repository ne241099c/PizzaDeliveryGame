using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum DragKind { Ticket, Dough, Ladle, Cheese, Topping, Pizza, Box, Bagged }

/// <summary>What the hand holds.</summary>
public sealed class DragPayload
{
    public DragKind Kind;public Order Order;public PizzaJob Job;public Topping Topping;public string Sprite;public Vector2 Size=new(140,140);
}
