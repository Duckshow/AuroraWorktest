using UnityEngine;
using System.Collections.Generic;

public enum CardinalDirection { None, North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest }
public enum Axis2D { None, Horizontal, Vertical }

public static class Utils
{
    public static CardinalDirection GetCardinalDirection(Transform pivot, Space space = Space.World)
    {
        float rot = space == Space.World ? pivot.eulerAngles.y : pivot.localEulerAngles.y;

        if (rot < 22.5f) { return CardinalDirection.North; }
        else if (rot < 67.5f) { return CardinalDirection.NorthEast; }
        else if (rot < 112.5f) { return CardinalDirection.East; }
        else if (rot < 157.5f) { return CardinalDirection.SouthEast; }
        else if (rot < 202.5f) { return CardinalDirection.South; }
        else if (rot < 247.5f) { return CardinalDirection.SouthWest; }
        else if (rot < 292.5) { return CardinalDirection.West; }
        else if (rot < 337.5f) { return CardinalDirection.NorthWest; }
        else { return CardinalDirection.North; }
    }

    public static Vector3 CardinalDirectionToVector3(CardinalDirection direction)
    {
        switch (direction)
        {
            case CardinalDirection.North: { return new Vector3(0f, 0f, 1f); }
            case CardinalDirection.NorthEast: { return new Vector3(1f, 0f, 1f); }
            case CardinalDirection.East: { return new Vector3(1f, 0f, 0f); }
            case CardinalDirection.SouthEast: { return new Vector3(1f, 0f, -1f); }
            case CardinalDirection.South: { return new Vector3(0f, 0f, -1f); }
            case CardinalDirection.SouthWest: { return new Vector3(-1f, 0f, -1f); }
            case CardinalDirection.West: { return new Vector3(-1f, 0f, 0f); }
            case CardinalDirection.NorthWest: { return new Vector3(-1f, 0f, 1f); }
            default: throw new System.NotImplementedException();
        }
    }

    public static CardinalDirection GetOppositeDirection(CardinalDirection direction)
    {
        switch (direction)
        {
            case CardinalDirection.North: { return CardinalDirection.South; }
            case CardinalDirection.NorthEast: { return CardinalDirection.SouthWest; }
            case CardinalDirection.East: { return CardinalDirection.West; }
            case CardinalDirection.SouthEast: { return CardinalDirection.NorthWest; }
            case CardinalDirection.South: { return CardinalDirection.North; }
            case CardinalDirection.SouthWest: { return CardinalDirection.NorthEast; }
            case CardinalDirection.West: { return CardinalDirection.East; }
            case CardinalDirection.NorthWest: { return CardinalDirection.SouthEast; }
            default: throw new System.NotImplementedException();
        }
    }

    public static void Shuffle<T>(this IList<T> ts) // source: https://forum.unity.com/threads/clever-way-to-shuffle-a-list-t-in-one-line-of-c-code.241052/
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
}
