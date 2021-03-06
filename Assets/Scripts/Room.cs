using UnityEngine;
using UnityEngine.Assertions;

public class Room : MonoBehaviour
{
    public const int MIN_ROOM_WIDTH = 2;
    public const int DEFAULT_ROOM_HEIGHT = 5;

    [SerializeField, HideInInspector] private RoomTile[] roomTiles;
    [SerializeField, HideInInspector] private Vector3Int dimensions;
    [SerializeField, HideInInspector] private Passage[] passages;

    public Vector3Int Dimensions { get { return dimensions; } }
    public Passage[] Passages { get { return passages; } }

    private void OnValidate()
    {
        roomTiles = GetComponentsInChildren<RoomTile>();
        dimensions = FindDimensions(roomTiles);
        passages = GetComponentsInChildren<Passage>();

        PerformSafetyChecks();
    }

    private static Vector3Int FindDimensions(RoomTile[] tiles)
    {
        Vector3Int dimensions = new Vector3Int();

        foreach (RoomTile piece in tiles)
        {
            Vector3 localPos = piece.transform.localPosition;

            if (localPos.x > dimensions.x) { dimensions.x = Mathf.FloorToInt(localPos.x); }
            if (localPos.z > dimensions.z) { dimensions.z = Mathf.FloorToInt(localPos.z); }
        }

        dimensions.y = DEFAULT_ROOM_HEIGHT;

        // we do not need to add 1 to to the dimensions like you normally might, because a room is surrounded by walls, which are 0.5 units thick (because they can overlap), thus making any room 1 unit smaller

        return dimensions;
    }

    private void PerformSafetyChecks()
    {
        foreach (RoomTile tile in roomTiles)
        {
            Vector3 tileLocalPos = tile.transform.localPosition;

            Assert.IsFalse(
                tileLocalPos.x < 0 || tileLocalPos.y < 0 || tileLocalPos.z < 0,
                string.Format("{0} has a {1} with a local position below 0!", transform.name, tile.Type)
            );
        }

        foreach (Passage passage in Passages)
        {
            Vector3 passageLocalPos = passage.transform.localPosition;

            bool isOnEdgeWest = passageLocalPos.x == 0;
            bool isOnEdgeEast = passageLocalPos.x == Dimensions.x;
            bool isOnEdgeNorth = passageLocalPos.z == Dimensions.z;
            bool isOnEdgeSouth = passageLocalPos.z == 0;

            bool isOnCornerSouthWest = isOnEdgeSouth && isOnEdgeWest;
            bool isOnCornerSouthEast = isOnEdgeSouth && isOnEdgeEast;
            bool isOnCornerNorthWest = isOnEdgeNorth && isOnEdgeWest;
            bool isOnCornerNorthEast = isOnEdgeNorth && isOnEdgeEast;

            Assert.IsFalse(isOnCornerNorthEast || isOnCornerSouthEast || isOnCornerSouthWest || isOnCornerNorthWest, string.Format("{0} has a Passage on a corner, which is not allowed!", transform.name));
            Assert.IsTrue(isOnEdgeWest || isOnEdgeEast || isOnEdgeNorth || isOnEdgeSouth, string.Format("{0} has a Passage that is not on the boundary of the room, which is not allowed!", transform.name));

            CardinalDirection dir = Utils.GetCardinalDirection(passage.transform);

            if (isOnEdgeWest) { Assert.AreEqual(CardinalDirection.West, dir, string.Format("{0} has a Passage on the Western edge, but it's facing {1}!", transform.name, dir)); }
            if (isOnEdgeEast) { Assert.AreEqual(CardinalDirection.East, dir, string.Format("{0} has a Passage on the Eastern edge, but it's facing {1}!", transform.name, dir)); }
            if (isOnEdgeNorth) { Assert.AreEqual(CardinalDirection.North, dir, string.Format("{0} has a Passage on the Northern edge, but it's facing {1}!", transform.name, dir)); }
            if (isOnEdgeSouth) { Assert.AreEqual(CardinalDirection.South, dir, string.Format("{0} has a Passage on the Southern edge, but it's facing {1}!", transform.name, dir)); }


            BoundingBox2D passageBox = BoundingBox2D.GetPassageBoundingBox(passage);

            foreach (Passage otherPassage in Passages)
            {
                if (otherPassage == passage)
                {
                    continue;
                }

                BoundingBox2D otherPassageBox = BoundingBox2D.GetPassageBoundingBox(otherPassage);
                Assert.IsFalse(BoundingBox2D.AreBoxesColliding(passageBox, otherPassageBox), string.Format("{0} has two Passages whose colliders are intersecting!", transform.name));
            }
        }
    }

    public bool HasUnconnectedPassages()
    {
        foreach (Passage passage in passages)
        {
            if (!passage.HasConnection)
            {
                return true;
            }
        }

        return false;
    }
}