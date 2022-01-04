using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using System.Threading.Tasks;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    private const string ROOM_PREFABS_ADDRESSABLE_LABEL = "Rooms";

    private List<Room> allRooms = new List<Room>();

    private RoomTile.TileType[,] grid;
    private Vector2Int gridDimensions;

    private void Start()
    {
        GenerateLevel();
    }

    private async void GenerateLevel()
    {
        gridDimensions = new Vector2Int(128, 64); // TODO: make sure numbers are multiple of two
        Assert.IsTrue(Utils.IsPowerOfTwo(gridDimensions.x) && Utils.IsPowerOfTwo(gridDimensions.y), string.Format("Level dimensions are {0}, but must be a power of two!", gridDimensions));

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

        BSPNode binarySpacePartition = new BSPNode(Vector2Int.zero, gridDimensions, minBSPSize);
        List<BSPNode> bottomNodes = new List<BSPNode>();
        binarySpacePartition.FindAllBottomNodes(bottomNodes);

        Room startRoom = allRooms[Random.Range(0, allRooms.Count)];
        BSPNode startNode = binarySpacePartition.FindBottomLeftMostNode();
        AddRoomToGrid(startRoom, startNode);

        Room endRoom = allRooms[Random.Range(0, allRooms.Count)];
        BSPNode endNode = binarySpacePartition.FindUpperRighttMostNode();
        AddRoomToGrid(endRoom, endNode);

        int extraRoomCount = 2;
        for (int i = 0; i < extraRoomCount; i++)
        {
            Room room = allRooms[Random.Range(0, allRooms.Count)];
            BSPNode node = BSPNode.FindRandomNodeWithoutRoom(bottomNodes);
            //AddRoomToGrid(room, node);
        }

        // 3. Loop x times
        //     3.1. Spawn random room at a random odd-numbered position with a random rotation
        //     3.2. While the added room has >1 door free, randomly choose to spawn and connect a new room door-to-door
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
                    texture.SetPixel(node.Pos.x + x, node.Pos.y + y, color);
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

    private void AddRoomToGrid(Room room, BSPNode node)
    {
        Vector2Int coords = node.Pos;

        for (int y = 0; y < room.Dimensions.y; y++)
        {
            for (int x = 0; x < room.Dimensions.x; x++)
            {
                Debug.Log(new Vector2Int(coords.x + x, coords.y + y) + ", " + room.Dimensions);
                grid[coords.x + x, coords.y + y] = room.TileMap[x, y];
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

            if (dimensions.x <= minSize || dimensions.y <= minSize)
            {
                return;
            }

            bool splitHorizontally = Random.value < 0.5f;

            Vector2Int childPos1 = pos;
            Vector2Int childPos2 = pos;
            Vector2Int childDimensions1 = dimensions;
            Vector2Int childDimensions2 = dimensions;

            if (splitHorizontally)
            {
                childDimensions1.y /= 2;
                childDimensions2.y /= 2;

                childPos2.y += childDimensions1.y;
            }
            else
            {
                childDimensions1.x /= 2;
                childDimensions2.x /= 2;

                childPos2.x += childDimensions1.x;
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