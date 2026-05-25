using System;
using UnityEngine;

[Serializable]
public struct FlavorVector
{
    public int sweet;
    public int spicy;
    public int sour;
    public int umami;
    public int texture;

    public static FlavorVector Zero => new FlavorVector
    {
        sweet = 0,
        spicy = 0,
        sour = 0,
        umami = 0,
        texture = 0
    };

    public static FlavorVector operator +(FlavorVector a, FlavorVector b) => new FlavorVector
    {
        sweet = a.sweet + b.sweet,
        spicy = a.spicy + b.spicy,
        sour = a.sour + b.sour,
        umami = a.umami + b.umami,
        texture = a.texture + b.texture
    };

    public static FlavorVector operator -(FlavorVector a, FlavorVector b) => new FlavorVector
    {
        sweet = a.sweet - b.sweet,
        spicy = a.spicy - b.spicy,
        sour = a.sour - b.sour,
        umami = a.umami - b.umami,
        texture = a.texture - b.texture
    };

    public int ManhattanDistance(FlavorVector other)
    {
        return Mathf.Abs(sweet - other.sweet)
             + Mathf.Abs(spicy - other.spicy)
             + Mathf.Abs(sour - other.sour)
             + Mathf.Abs(umami - other.umami)
             + Mathf.Abs(texture - other.texture);
    }

    public override string ToString()
    {
        return $"Swt:{sweet}, Spc:{spicy}, Sur:{sour}, Uma:{umami}, Tex:{texture}";
    }
}