using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;

public class LevelGenerator : MonoBehaviour
{
    private const string ROOM_PREFABS_ADDRESSABLE_LABEL = "Rooms";
    private const int EXTRA_GRID_SPACE_ALONG_EDGES = 2; // this is to leave space for corridors all the way around rooms

    private List<Room> allRooms = new List<Room>();

    private RoomTile.TileType[,] grid;
    private Vector2Int gridDimensions;

    [SerializeField] private int seed = 92985421;

    struct PathfinderStep
    {
        public Vector2Int NewCoords { get; private set; }
        public Vector2Int[] PreviousCoords { get; private set; }

        public PathfinderStep(Vector2Int currentCoords)
        {
            NewCoords = currentCoords;
            PreviousCoords = null;
        }

        public PathfinderStep(Vector2Int currentCoords, CardinalDirection stepDirection, Vector2Int[] previousCoords)
        {
            PreviousCoords = Utils.AddToEndOfArray<Vector2Int>(previousCoords, currentCoords);
            NewCoords = currentCoords + Utils.CardinalDirectionToVector(stepDirection);
        }
    }

    private void Start()
    {
        GenerateLevel();
    }

    private async void GenerateLevel()
    {
        gridDimensions = new Vector2Int(128, 64); // TODO: make sure numbers are multiple of two
        Assert.IsTrue(Utils.IsPowerOfTwo(gridDimensions.x) && Utils.IsPowerOfTwo(gridDimensions.y), string.Format("Level dimensions are {0}, but must be a power of two!", gridDimensions));

        Vector2Int extraSpaceDimensions = new Vector2Int(EXTRA_GRID_SPACE_ALONG_EDGES * 2, EXTRA_GRID_SPACE_ALONG_EDGES * 2);
        gridDimensions += extraSpaceDimensions;

        grid = new RoomTile.TileType[gridDimensions.x, gridDimensions.y];

        await Addressables.LoadAssetsAsync<GameObject>(ROOM_PREFABS_ADDRESSABLE_LABEL, (GameObject loadedPrefab) => { allRooms.Add(loadedPrefab.GetComponent<Room>()); }).Task;

        if (gameObject == null) // not sure if necessary, but some older forum posts mention that this *has* been a necessary precaution when using async-await
        {
            return;
        }

        int maxRoomDimension = 0;
        for (int i = 0; i < allRooms.Count; i++)
        {
            Vector2Int roomDimensions = allRooms[i].Dimensions;
            maxRoomDimension = Mathf.Max(maxRoomDimension, roomDimensions.x);
            maxRoomDimension = Mathf.Max(maxRoomDimension, roomDimensions.y);
        }

        int minBSPSize = Utils.RoundUpToPowerOfTwo(maxRoomDimension);

        Debug.Log(minBSPSize);

        BSPNode binarySpacePartition = new BSPNode(Vector2Int.zero, gridDimensions - extraSpaceDimensions, minBSPSize);
        List<BSPNode> bottomNodes = new List<BSPNode>();
        binarySpacePartition.FindAllBottomNodes(bottomNodes);

        List<Room> roomInstances = new List<Room>();

        int passageCount = 0;

        BSPNode startNode = binarySpacePartition.FindBottomLeftMostNode();
        AddRandomRoom(startNode, ref passageCount, roomInstances);

        int pathRoomCount = 4;
        for (int i = 0; i < pathRoomCount; i++)
        {
            /*
                TODO: 
                When every room has been spawned, spawn additional rooms for every door without a possible connection
            */

            BSPNode node = BSPNode.FindRandomNodeWithoutRoom(bottomNodes);
            AddRandomRoom(node, ref passageCount, roomInstances, minPassageCount: 2);
        }

        BSPNode endNode = binarySpacePartition.FindUpperRighttMostNode();
        AddRandomRoom(endNode, ref passageCount, roomInstances);

        for (int i = 0; i < roomInstances.Count - 1; i++)
        {
            Room roomInstance = roomInstances[i];
            Room nextRoomInstance = roomInstances[i + 1];

            float closestPassageDistance = float.MaxValue;
            RoomTile closestPassage1 = null;
            RoomTile closestPassage2 = null;

            for (int pIndex1 = 0; pIndex1 < roomInstance.Passages.Length; pIndex1++)
            {
                RoomTile roomPassage = roomInstance.Passages[pIndex1];
                Vector3 roomPassagePos = roomInstance.transform.localPosition + roomPassage.transform.localPosition;

                for (int pIndex2 = 0; pIndex2 < nextRoomInstance.Passages.Length; pIndex2++)
                {
                    RoomTile nextRoomPassage = nextRoomInstance.Passages[pIndex2];
                    Vector3 nextRoomPassagePos = nextRoomInstance.transform.localPosition + nextRoomPassage.transform.localPosition;

                    float distance = (nextRoomPassagePos - roomPassagePos).sqrMagnitude;
                    if (distance < closestPassageDistance)
                    {
                        closestPassageDistance = distance;
                        closestPassage1 = roomPassage;
                        closestPassage2 = nextRoomPassage;
                    }
                }
            }

            Vector3 passagePos1 = roomInstance.transform.localPosition + closestPassage1.transform.localPosition;
            Vector3 passagePos2 = nextRoomInstance.transform.localPosition + closestPassage2.transform.localPosition;

            Vector2Int startCoords = new Vector2Int(Mathf.FloorToInt(passagePos1.x), Mathf.FloorToInt(passagePos1.z)) + Utils.CardinalDirectionToVector(closestPassage1.GetCardinalDirection());
            Vector2Int endCoords = new Vector2Int(Mathf.FloorToInt(passagePos2.x), Mathf.FloorToInt(passagePos2.z)) + Utils.CardinalDirectionToVector(closestPassage2.GetCardinalDirection());

            Vector2Int[] path;

            Queue<PathfinderStep> stepsToEvaluate = new Queue<PathfinderStep>();
            stepsToEvaluate.Enqueue(new PathfinderStep(startCoords));

            while (stepsToEvaluate.Count > 0)
            {
                PathfinderStep step = stepsToEvaluate.Dequeue();

                if (step.NewCoords == endCoords)
                {
                    path = Utils.AddToEndOfArray<Vector2Int>(step.PreviousCoords, step.NewCoords);
                    break;
                }

                RoomTile.TileType type = grid[step.NewCoords.x, step.NewCoords.y];
                if (type != RoomTile.TileType.None)
                {
                    continue;
                }

                float distanceFromNorth = (endCoords - (step.NewCoords + new Vector2Int(0, 1))).sqrMagnitude;
                float distanceFromEast = (endCoords - (step.NewCoords + new Vector2Int(1, 0))).sqrMagnitude;
                float distanceFromSouth = (endCoords - (step.NewCoords + new Vector2Int(0, -1))).sqrMagnitude;
                float distanceFromWest = (endCoords - (step.NewCoords + new Vector2Int(-1, 0))).sqrMagnitude;

                // TODO: I think I might have to start over
            }

            // find the closest path between the doors
            // mark the path as floor
            // mark the doors as connected

        }





        for (int y = 0; y < gridDimensions.y; y++)
        {
            for (int x = 0; x < gridDimensions.x; x++)
            {
                RoomTile.TileType tileType = grid[x, y];
                if (tileType == RoomTile.TileType.None)
                {
                    grid[x, y] = RoomTile.TileType.Wall;
                }
                else



                    RoomTile.TileType tileTypeNorth = grid[x, y + 1];
                RoomTile.TileType tileTypeEast = grid[x + 1, y];
                RoomTile.TileType tileSouth = grid[x, y - 1];
                RoomTile.TileType tileWest = grid[x - 1, y];
            }
        }

        // 4. Iterate over grid
        //     4.1. If tile is empty, run a maze algorithm from that point
        // 5. Find every non-room tile touching 3 walls and loop over them
        //     5.1. If the tile is neighboring a door, or is not touching 3 walls, skip it
        //     5.2 Else, remove the tile and add its free neighbor to the loop
        // 6. Loop over grid and instantiate the corresponding prefabs to the scene


        Texture2D texture = new Texture2D(gridDimensions.x, gridDimensions.y);

        for (int i = 0; i < bottomNodes.Count; i++)
        {
            BSPNode node = bottomNodes[i];
            Color[] colors = new Color[] {
                Color.red,
                Color.yellow,
                Color.green,
                Color.cyan,
                Color.blue,
                Color.magenta
            };

            Color color = colors[Random.Range(0, colors.Length)];

            for (int y = 0; y < node.Dimensions.y; y++)
            {
                for (int x = 0; x < node.Dimensions.x; x++)
                {
                    //Debug.Log((node.Pos.x + x) + ", " + (node.Pos.y + y));
                    texture.SetPixel(node.Pos.x + x + EXTRA_GRID_SPACE_ALONG_EDGES, node.Pos.y + y + EXTRA_GRID_SPACE_ALONG_EDGES, color);
                }
            }
        }

        for (int y = 0; y < gridDimensions.y; y++)
        {
            for (int x = 0; x < gridDimensions.x; x++)
            {
                RoomTile.TileType tileType = grid[x, y];

                if (tileType == RoomTile.TileType.None)
                {
                    continue;
                }

                Color color = Color.clear;
                switch (grid[x, y])
                {
                    case RoomTile.TileType.Floor: { color = Color.white; break; }
                    case RoomTile.TileType.Wall: { color = Color.grey; break; }
                    case RoomTile.TileType.Passage: { color = Color.red; break; }
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.Apply();
        GetComponent<MeshRenderer>().material.mainTexture = texture;
        transform.localScale = new Vector3(gridDimensions.x / 10f, gridDimensions.y / 10f, 1f);
    }

    private void AddRandomRoom(BSPNode node, ref int spawnedPassageCount, List<Room> roomInstances, int minPassageCount = 1)
    {
        Room room;
        if (minPassageCount < 2)
        {
            room = allRooms[Random.Range(0, allRooms.Count)];
        }
        else
        {
            IEnumerable<Room> roomQuery = allRooms.Where((Room someRoom) => someRoom.Passages.Length >= 2);
            roomQuery.OrderBy(x => Random.Range(0, allRooms.Count));
            room = roomQuery.First();
        }

        spawnedPassageCount += room.Passages.Length;

        Room roomInstance = Instantiate(room.gameObject, transform).GetComponent<Room>();
        roomInstances.Add(roomInstance);
        roomInstance.transform.localPosition = new Vector3(node.Pos.x, 0f, node.Pos.y);


        Vector2Int coords = node.Pos;
        for (int y = 0; y < room.Dimensions.y; y++)
        {
            for (int x = 0; x < room.Dimensions.x; x++)
            {
                // TODO: randomize rotation

                grid[coords.x + x + EXTRA_GRID_SPACE_ALONG_EDGES, coords.y + y + EXTRA_GRID_SPACE_ALONG_EDGES] = room.TileMap[x, y];
            }
        }

        node.HasBeenAssignedRoom = true;
    }

    private class BSPNode
    {
        public Vector2Int Pos { get; private set; }
        public Vector2Int Dimensions { get; private set; }
        public bool HasBeenAssignedRoom;

        public bool IsBottomNode { get; private set; }
        public BSPNode Child1 { get; private set; }
        public BSPNode Child2 { get; private set; }

        public BSPNode(Vector2Int pos, Vector2Int dimensions, int minSize)
        {
            Assert.IsTrue(Utils.IsPowerOfTwo(minSize), string.Format("BSPNode's MinSize is {0}, but has to be a power of two!", minSize));

            Pos = pos;
            Dimensions = dimensions;
            HasBeenAssignedRoom = false;
            IsBottomNode = true;

            Axis2D splitAxis = Axis2D.None;
            if (dimensions.x >= minSize * 2) { splitAxis = Axis2D.Vertical; }
            else if (dimensions.y >= minSize * 2) { splitAxis = Axis2D.Horizontal; }
            else { return; }

            Vector2Int childPos1 = pos;
            Vector2Int childPos2 = pos;
            Vector2Int childDimensions1 = dimensions;
            Vector2Int childDimensions2 = dimensions;

            if (splitAxis == Axis2D.Vertical)
            {
                childDimensions1.x /= 2;
                childDimensions2.x /= 2;

                childPos2.x += childDimensions1.x;
            }
            else
            {
                childDimensions1.y /= 2;
                childDimensions2.y /= 2;

                childPos2.y += childDimensions1.y;
            }

            IsBottomNode = false;
            Child1 = new BSPNode(childPos1, childDimensions1, minSize);
            Child2 = new BSPNode(childPos2, childDimensions2, minSize);
        }

        public BSPNode FindBottomLeftMostNode()
        {
            if (IsBottomNode)
            {
                return this;
            }

            BSPNode bottomLeftChild = (Child1.Pos.x < Child2.Pos.x || Child1.Pos.y < Child2.Pos.y) ? Child1 : Child2;
            return bottomLeftChild.FindBottomLeftMostNode();
        }

        public BSPNode FindUpperRighttMostNode()
        {
            if (IsBottomNode)
            {
                return this;
            }

            BSPNode upperRightChild = (Child2.Pos.x > Child1.Pos.x || Child2.Pos.y > Child1.Pos.y) ? Child2 : Child1;
            return upperRightChild.FindUpperRighttMostNode();
        }

        public void FindAllBottomNodes(List<BSPNode> bottomNodes)
        {
            if (IsBottomNode)
            {
                bottomNodes.Add(this);
                return;
            }

            Child1.FindAllBottomNodes(bottomNodes);
            Child2.FindAllBottomNodes(bottomNodes);
        }

        public static BSPNode FindRandomNodeWithoutRoom(List<BSPNode> bottomNodes)
        {
            BSPNode node = null;
            do
            {
                node = bottomNodes[Random.Range(0, bottomNodes.Count)];
            } while (node == null || node.HasBeenAssignedRoom);

            return node;
        }
    }
}