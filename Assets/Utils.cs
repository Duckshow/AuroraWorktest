using UnityEngine;

public enum CardinalDirection { None, North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest }

public static class Utils
{
    public static bool IsOdd(float x) { return x % 2 == 1; }

    public static Vector2Int CardinalDirectionToVector(CardinalDirection dir)
    {
        switch (dir)
        {
            case CardinalDirection.North:
                return Vector2Int.up;
            case CardinalDirection.NorthEast:
                return Vector2Int.up + Vector2Int.right;
            case CardinalDirection.East:
                return Vector2Int.right;
            case CardinalDirection.SouthEast:
                return Vector2Int.down + Vector2Int.right;
            case CardinalDirection.South:
                return Vector2Int.down;
            case CardinalDirection.SouthWest:
                return Vector2Int.down + Vector2Int.left;
            case CardinalDirection.West:
                return Vector2Int.left;
            case CardinalDirection.NorthWest:
                return Vector2Int.up + Vector2Int.left;
            default:
                return Vector2Int.zero;
        }
    }
}
