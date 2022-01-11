using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Linq;
using System;
using Random = UnityEngine.Random;

public class LevelGenerator : MonoBehaviour
{
    private enum RoomType { None, Start, Path, End, Branch }

    private const string ROOM_PREFABS_ADDRESSABLE_LABEL = "Rooms";
    private const int MAX_PASSAGES_ALLOWED_AT_START = 1;
    private const int MAX_PASSAGES_ALLOWED_AT_END = 1;

    [SerializeField] private bool useRandomSeed;
    [SerializeField] private int seed = 92985421;

    [Space]
    [SerializeField] private int amountOfRoomsOnMainPath;
    [SerializeField] private int maxBranchDepth = 3;

    [Space]
    [SerializeField] private Transform playerInstance;

    private List<Room> spawnedRooms = new List<Room>();
    private List<Room> roomPrefabs = new List<Room>();
    private List<Room> pathRoomPrefabs;
    private List<Room> deadEndRoomPrefabs;

    private void OnValidate()
    {
        amountOfRoomsOnMainPath = Mathf.Max(2, amountOfRoomsOnMainPath);
        maxBranchDepth = Mathf.Max(1, maxBranchDepth);
    }

    private void Awake()
    {
        SetRandomSeed();
    }

    private void SetRandomSeed()
    {
        int seedToUse = useRandomSeed ? seed : Random.Range(int.MinValue, int.MaxValue); // the point of this is just to expose the seed in the editor, so we can save it if we want

        Debug.LogFormat("SEED: {0}", seedToUse);
        Random.InitState(seedToUse);
    }

    private void Start()
    {
        LoadAssets(onFinished: () =>
        {
            GenerateLevel();
        });
    }

    private async void LoadAssets(Action onFinished)
    {
        await Addressables.LoadAssetsAsync<GameObject>(ROOM_PREFABS_ADDRESSABLE_LABEL, (GameObject loadedPrefab) =>
        {
            roomPrefabs.Add(loadedPrefab.GetComponent<Room>());
        }).Task;

        if (gameObject == null) // not sure if necessary, but some older forum posts mention that this *has* been a necessary precaution when using async-await
        {
            return;
        }

        if (onFinished != null)
        {
            onFinished();
        }
    }

    private void GenerateLevel()
    {
        Room lastSpawnedRoom = null;
        CardinalDirection forbiddenDirection = CardinalDirection.None;

        for (int i = 0; i < amountOfRoomsOnMainPath; i++)
        {
            RoomType roomType = RoomType.None;

            if (i == 0)
            {
                roomType = RoomType.Start;
            }
            else if (i == amountOfRoomsOnMainPath - 1)
            {
                roomType = RoomType.End;
            }
            else
            {
                roomType = RoomType.Path;
            }

            lastSpawnedRoom = AddRoom(roomType, GetRoomPrefabsToChooseFrom(roomType), lastSpawnedRoom, forbiddenDirection);

            if (i == 1)
            {
                foreach (Passage passage in GetStartRoom().Passages)
                {
                    if (passage.HasConnection)
                    {
                        forbiddenDirection = Utils.GetOppositeDirection(Utils.GetCardinalDirection(passage.transform)); // by forbidding the initial opposite direction, we should be able to prevent the level from curling back in on itself
                        continue;
                    }
                }

            }
        }

        List<Room> previouslySpawnedRooms = new List<Room>(spawnedRooms);
        List<Room> newlySpawnedRooms = new List<Room>();

        for (int i = 0; i < maxBranchDepth; i++)
        {
            List<Room> branchRoomPrefabs = GetRoomPrefabsToChooseFrom(RoomType.Branch, branchDepth: i);

            foreach (Room room in previouslySpawnedRooms)
            {

                while (room.HasUnconnectedPassages())
                {
                    Room branchRoom = AddRoom(RoomType.Branch, branchRoomPrefabs, room, forbiddenDirection: CardinalDirection.None);
                    newlySpawnedRooms.Add(branchRoom);
                }
            }

            previouslySpawnedRooms.Clear();
            previouslySpawnedRooms.AddRange(newlySpawnedRooms);
            newlySpawnedRooms.Clear();
        }

        if (playerInstance != null)
        {
            Room startRoom = GetStartRoom();
            playerInstance.transform.position = startRoom.transform.position + new Vector3(startRoom.Dimensions.x / 2f, 1f, startRoom.Dimensions.z / 2f); // TODO: replace this with an actual spawn point
        }
    }

    private Room AddRoom(RoomType roomType, List<Room> prefabsToChooseFrom, Room roomToConnectTo, CardinalDirection forbiddenDirection)
    {
        prefabsToChooseFrom.Shuffle();

        foreach (Room newRoomPrefab in prefabsToChooseFrom)
        {
            // TODO: would be safer to use Addressables.Instantiate, memory-management wise - but we have to remove LoadAssetsAsync then, if that's possible 
            Room newRoom = Instantiate(newRoomPrefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<Room>();
            newRoom.transform.SetParent(transform);

            if (roomToConnectTo != null)
            {

                if (!TryConnectRooms(roomToConnectTo, newRoom, forbiddenDirection))
                {
                    Destroy(newRoom.gameObject);
                    continue;
                }
            }

            newRoom.name = roomType.ToString() + newRoom.name;

            if (roomType == RoomType.Branch)
            {
                spawnedRooms.Insert(spawnedRooms.Count - 2, newRoom);
            }
            else
            {
                spawnedRooms.Add(newRoom);
            }

            return newRoom;
        }

        throw new System.Exception(string.Format("LevelGenerator failed to add a room, which would connect to {0}!", roomToConnectTo.name));
    }

    private List<Room> GetRoomPrefabsToChooseFrom(RoomType roomType, int branchDepth = -1)
    {
        switch (roomType)
        {
            case RoomType.Start:
                {
                    return roomPrefabs.Where(x => x.Passages.Length == MAX_PASSAGES_ALLOWED_AT_START).ToList();
                }
            case RoomType.End:
                {
                    return roomPrefabs.Where(x => x.Passages.Length == MAX_PASSAGES_ALLOWED_AT_END).ToList();
                }
            case RoomType.Path:
                {
                    if (pathRoomPrefabs == null || pathRoomPrefabs.Count == 0)
                    {
                        pathRoomPrefabs = roomPrefabs.Where(x => x.Passages.Length >= 2).ToList();
                    }

                    return pathRoomPrefabs;
                }
            case RoomType.Branch:
                {
                    if (branchDepth == maxBranchDepth - 1)
                    {
                        if (deadEndRoomPrefabs == null || deadEndRoomPrefabs.Count == 0)
                        {
                            deadEndRoomPrefabs = roomPrefabs.Where(x => x.Passages.Length == 1).ToList(); // TODO: would be nice if the amount of doors randomly tapered off instead
                        }

                        return deadEndRoomPrefabs;
                    }

                    return roomPrefabs;
                }
            default: throw new System.NotImplementedException();
        }
    }

    private Room GetStartRoom()
    {
        return spawnedRooms[0];
    }

    private bool TryConnectRooms(Room oldRoom, Room newRoom, CardinalDirection forbiddenDirection)
    {
        List<RoomTile> oldRoomPassages = new List<RoomTile>(oldRoom.Passages);
        oldRoomPassages.Shuffle();

        List<RoomTile> newRoomPassages = new List<RoomTile>(newRoom.Passages);
        newRoomPassages.Shuffle();

        foreach (Passage oldRoomPassage in oldRoomPassages)
        {
            if (oldRoomPassage.HasConnection)
            {
                continue;
            }

            if (Utils.GetCardinalDirection(oldRoomPassage.transform) == forbiddenDirection)
            {
                continue;
            }

            foreach (Passage newRoomPassage in newRoomPassages)
            {
                CardinalDirection oldPassageDirection = Utils.GetCardinalDirection(oldRoomPassage.transform);
                CardinalDirection newPassageDirection = Utils.GetCardinalDirection(newRoomPassage.transform);
                CardinalDirection desiredDirection = Utils.GetOppositeDirection(oldPassageDirection);

                float degrees = Utils.GetDegreesBetweenDirections(newPassageDirection, desiredDirection);
                newRoom.transform.Rotate(0f, degrees, 0f, Space.World);

                Vector3 newPassageOffset = newRoomPassage.transform.position - newRoom.transform.position;
                newRoom.transform.position = oldRoomPassage.transform.position - newPassageOffset;

                if (newRoom.Passages.Length > 1 && !DoesRoomHaveOtherPassageNotPointingInDirection(newRoom, newRoomPassage, forbiddenDirection))
                {
                    continue;
                }

                if (IsRoomCollidingWithAnything(newRoom, ownPassageToIgnore: newRoomPassage, ignoreRoom: oldRoom))
                {
                    continue;
                }

                oldRoomPassage.HasConnection = true;
                newRoomPassage.HasConnection = true;
                return true;
            }
        }

        return false;
    }

    private static bool DoesRoomHaveOtherPassageNotPointingInDirection(Room room, Passage ignorePassage, CardinalDirection forbiddenDirection)
    {
        if (forbiddenDirection == CardinalDirection.None)
        {
            return true;
        }

        foreach (Passage passage in room.Passages)
        {
            if (passage == ignorePassage)
            {
                continue;
            }

            if (Utils.GetCardinalDirection(passage.transform) != forbiddenDirection)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsRoomCollidingWithAnything(Room room, Passage ownPassageToIgnore, Room ignoreRoom)
    {
        BoundingBox2D newRoomBox = BoundingBox2D.GetRoomBoundingBox(room);

        foreach (Room otherRoom in spawnedRooms)
        {
            if (otherRoom == ignoreRoom)
            {
                continue;
            }

            BoundingBox2D otherRoomBox = BoundingBox2D.GetRoomBoundingBox(otherRoom);

            if (BoundingBox2D.AreBoxesColliding(newRoomBox, otherRoomBox))
            {
                return true;
            }

            foreach (Passage otherPassage in otherRoom.Passages)
            {
                if (otherPassage.HasConnection)
                {
                    continue;
                }

                if (BoundingBox2D.AreBoxesColliding(newRoomBox, BoundingBox2D.GetPassageBoundingBox(otherPassage)))
                {
                    return true;
                }
            }

            foreach (Passage newPassage in room.Passages)
            {
                if (newPassage == ownPassageToIgnore)
                {
                    continue;
                }

                BoundingBox2D newPassageBox = BoundingBox2D.GetPassageBoundingBox(newPassage);

                if (BoundingBox2D.AreBoxesColliding(newPassageBox, otherRoomBox))
                {
                    return true;
                }

                foreach (Passage otherPassage in otherRoom.Passages)
                {
                    if (otherPassage.HasConnection)
                    {
                        continue;
                    }

                    if (BoundingBox2D.AreBoxesColliding(newPassageBox, BoundingBox2D.GetPassageBoundingBox(otherPassage)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}