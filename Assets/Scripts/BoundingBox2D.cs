using UnityEngine;

readonly struct BoundingBox2D
{
    public readonly Vector2Int BottomLeftCorner;
    public readonly Vector2Int Dimensions;

    public readonly Vector2Int TopRightCorner;
    public readonly Vector2Int TopLeftCorner;
    public readonly Vector2Int BottomRightCorner;

    public BoundingBox2D(Vector2Int bottomLeftCorner, Vector2Int dimensions)
    {
        BottomLeftCorner = bottomLeftCorner;
        Dimensions = dimensions;

        TopRightCorner = BottomLeftCorner + Dimensions - Vector2Int.one;
        TopLeftCorner = BottomLeftCorner + new Vector2Int(0, Dimensions.y - 1);
        BottomRightCorner = BottomLeftCorner + new Vector2Int(Dimensions.x - 1, 0);
    }

    public static BoundingBox2D GetRoomBoundingBox(Room room, bool debug = false)
    {
        CardinalDirection dir = Utils.GetCardinalDirection(room.transform);

        Vector2Int newBottomLeftCorner = new Vector2Int(Mathf.RoundToInt(room.transform.position.x), Mathf.RoundToInt(room.transform.position.z));
        Vector2Int rotatedDimensions = new Vector2Int(room.Dimensions.x, room.Dimensions.z);

        switch (dir)
        {
            case CardinalDirection.North:
                {
                    break;
                }
            case CardinalDirection.East:
                {
                    newBottomLeftCorner.y -= room.Dimensions.x;
                    rotatedDimensions = rotatedDimensions.SwapComponents();
                    break;
                }
            case CardinalDirection.South:
                {
                    newBottomLeftCorner.x -= room.Dimensions.x;
                    newBottomLeftCorner.y -= room.Dimensions.z;
                    break;
                }
            case CardinalDirection.West:
                {
                    newBottomLeftCorner.x -= room.Dimensions.z;
                    rotatedDimensions = rotatedDimensions.SwapComponents();
                    break;
                }
            default: throw new System.NotImplementedException();
        }

        return new BoundingBox2D(newBottomLeftCorner, rotatedDimensions);
    }

    public static BoundingBox2D GetPassageBoundingBox(Passage passage)
    {
        CardinalDirection dir = Utils.GetCardinalDirection(passage.transform);

        Vector2Int newBottomLeftCorner = new Vector2Int(Mathf.RoundToInt(passage.transform.position.x), Mathf.RoundToInt(passage.transform.position.z));

        int halfMinWidth = Room.MIN_ROOM_WIDTH / 2;

        switch (dir)
        {
            case CardinalDirection.North:
                {
                    newBottomLeftCorner.x -= halfMinWidth;
                    break;
                }
            case CardinalDirection.East:
                {
                    newBottomLeftCorner.y -= halfMinWidth;
                    break;
                }
            case CardinalDirection.South:
                {
                    newBottomLeftCorner.x -= halfMinWidth;
                    newBottomLeftCorner.y -= Room.MIN_ROOM_WIDTH;
                    break;
                }
            case CardinalDirection.West:
                {
                    newBottomLeftCorner.x -= Room.MIN_ROOM_WIDTH;
                    newBottomLeftCorner.y -= halfMinWidth;
                    break;
                }
            default: throw new System.NotImplementedException();
        }

        return new BoundingBox2D(newBottomLeftCorner, new Vector2Int(Room.MIN_ROOM_WIDTH, Room.MIN_ROOM_WIDTH));
    }

    public static bool AreBoxesColliding(BoundingBox2D box1, BoundingBox2D box2)
    {
        return
        box1.BottomLeftCorner.x <= box2.TopRightCorner.x &&
        box2.BottomLeftCorner.x <= box1.TopRightCorner.x &&
        box1.BottomLeftCorner.y <= box2.TopRightCorner.y &&
        box2.BottomLeftCorner.y <= box1.TopRightCorner.y;
    }
}