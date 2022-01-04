using UnityEngine;
using UnityEngine.Assertions;

public class Room : MonoBehaviour
{
    [SerializeField, HideInInspector] private RoomTile[] roomTiles;
    [SerializeField, HideInInspector] private Vector2Int dimensions;
    [SerializeField, HideInInspector] private RoomTile.TileType[,] tileMap;

    public Vector2Int Dimensions { get { return dimensions; } }
    public RoomTile.TileType[,] TileMap { get { return tileMap; } }

    private void OnValidate()
    {
        roomTiles = GetComponentsInChildren<RoomTile>();
        dimensions = FindDimensions(roomTiles);
        tileMap = new RoomTile.TileType[dimensions.x, dimensions.y];

        FillTileMap(roomTiles, ref tileMap);

        PerformSafetyChecks();
    }

    private static Vector2Int FindDimensions(RoomTile[] tiles)
    {
        Vector2Int dimensions = new Vector2Int();

        foreach (RoomTile piece in tiles)
        {
            Vector3 localPos = piece.transform.localPosition;

            if (localPos.x > dimensions.x) { dimensions.x = Mathf.FloorToInt(localPos.x); }
            if (localPos.z > dimensions.y) { dimensions.y = Mathf.FloorToInt(localPos.z); }
        }

        dimensions.x++;
        dimensions.y++;

        return dimensions;
    }

    private static void FillTileMap(RoomTile[] tiles, ref RoomTile.TileType[,] tileMap)
    {
        foreach (RoomTile piece in tiles)
        {
            Vector2Int coords = piece.GetCoordinatesInRoom();

            if (coords.x < 0 || coords.y < 0)
            {
                continue; // out-of-bounds coordinates will be flagged in PerformSafetyChecks(), but we have to get through this function first
            }

            tileMap[coords.x, coords.y] = piece.Type;
        }
    }

    private void PerformSafetyChecks()
    {
        for (int i = 0; i < roomTiles.Length; i++)
        {
            RoomTile tile = roomTiles[i];
            Vector2Int coords = tile.GetCoordinatesInRoom();

            Assert.IsFalse(
                coords.x < 0 || coords.y < 0,
                string.Format("{0} has a {1} with a local position below 0!", transform.name, tile.Type)
            );

            RoomTile.TileType tileType = tileMap[coords.x, coords.y];
            if (tileType != RoomTile.TileType.Passage) // TODO: if adding doors, add check for Door-type here
            {
                continue;
            }

            CardinalDirection direction = tile.GetCardinalDirection();
            Vector2Int neighborCoords = coords + Utils.CardinalDirectionToVector(direction);

            if (neighborCoords.x > 0 && neighborCoords.x < dimensions.x && neighborCoords.y > 0 && neighborCoords.y < dimensions.y)
            {
                Assert.IsTrue(
                    tileMap[neighborCoords.x, neighborCoords.y] == RoomTile.TileType.None,
                    string.Format("{0} has a Door/Passage that is not connected to empty space! This will break the level generator!", transform.name)
                );
            }
        }

        Assert.IsTrue(
            Utils.IsOdd(dimensions.x) && Utils.IsOdd(dimensions.y),
            string.Format("{0} has dimensions {1}, but room dimensions have to be odd!", transform.name, dimensions)
        );
    }
}
