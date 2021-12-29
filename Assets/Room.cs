using UnityEngine;
using UnityEngine.Assertions;

public class Room : MonoBehaviour
{
    public RoomTile.TileType[,] CreateTileMap()
    {
        RoomTile[] tiles = GetComponentsInChildren<RoomTile>();

        int width, length;
        GetDimensions(tiles, out width, out length);

        Assert.IsTrue(Utils.IsOdd(width), string.Format("{0} is {1} tiles wide, but room dimensions have to be odd!", gameObject.name, width));
        Assert.IsTrue(Utils.IsOdd(length), string.Format("{0} is {1} tiles long, but room dimensions have to be odd!", gameObject.name, length));

        RoomTile.TileType[,] tileMap = new RoomTile.TileType[width, length];
        FillTileMap(tiles, ref tileMap);

        return tileMap;
    }

    private static void GetDimensions(RoomTile[] tiles, out int width, out int length)
    {
        width = 0;
        length = 0;

        foreach (RoomTile piece in tiles)
        {
            Vector2 pos = piece.transform.position;

            if (pos.x > width) { width = Mathf.FloorToInt(pos.x); }
            if (pos.y > length) { length = Mathf.FloorToInt(pos.y); }
        }
    }

    private static void FillTileMap(RoomTile[] tiles, ref RoomTile.TileType[,] tileMap)
    {
        foreach (RoomTile piece in tiles)
        {
            Vector2 pos = piece.transform.position;

            Vector2Int coords = new Vector2Int(
                Mathf.FloorToInt(pos.x),
                Mathf.FloorToInt(pos.y)
            );

            tileMap[coords.x, coords.y] = piece.Type;
        }
    }
}
