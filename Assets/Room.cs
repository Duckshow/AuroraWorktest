using UnityEngine;

public class Room : MonoBehaviour
{
    public RoomTile.TileType[,] CreateTileMap()
    {
        RoomTile[] tiles = GetComponentsInChildren<RoomTile>();

        int width, height;
        GetDimensions(tiles, out width, out height);

        RoomTile.TileType[,] tileMap = new RoomTile.TileType[width, height];
        FillTileMap(tiles, ref tileMap);

        return tileMap;
    }

    private static void GetDimensions(RoomTile[] tiles, out int width, out int height)
    {
        width = 0;
        height = 0;

        foreach (RoomTile piece in tiles)
        {
            Vector2 pos = piece.transform.position;

            if (pos.x > width) { width = Mathf.FloorToInt(pos.x); }
            if (pos.y > height) { height = Mathf.FloorToInt(pos.y); }
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
