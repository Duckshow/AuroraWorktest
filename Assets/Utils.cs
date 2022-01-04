using UnityEngine;

public enum CardinalDirection { None, North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest }

public static class Utils
{
    public static bool IsOdd(float x) { return x % 2 == 1; }
    public static bool IsPowerOfTwo(int x) { return (x != 0) && ((x & (x - 1)) == 0); }
    public static int RoundUpToPowerOfTwo(int x)
    {
        x--;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        x++;
        return x;
    }

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
