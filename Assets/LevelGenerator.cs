using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class LevelGenerator : MonoBehaviour
{
    private const string ROOM_PREFABS_ADDRESSABLE_LABEL = "Rooms";
    private const int MAX_PASSAGES_ALLOWED_AT_START = 1;
    private const int MAX_PASSAGES_ALLOWED_AT_END = 1;

    [SerializeField] private bool useRandomSeed;
    [SerializeField] private int seed = 92985421;

    [Space]
    [SerializeField] private int roomsOnPath;
    [SerializeField] private int maxBranchDepth = 3;

    [Space]
    [SerializeField] private Transform playerInstance;

    private List<Room> roomPrefabs = new List<Room>();

    private void OnValidate()
    {
        roomsOnPath = Mathf.Max(1, roomsOnPath);
        maxBranchDepth = Mathf.Max(1, maxBranchDepth);
    }

    private void Start()
    {
        if (useRandomSeed)
        {
            Random.InitState(seed);
        }

        StartCoroutine(GenerateLevel());
    }

    private IEnumerator GenerateLevel()
    {
        yield return Addressables.LoadAssetsAsync<GameObject>(ROOM_PREFABS_ADDRESSABLE_LABEL, (GameObject loadedPrefab) => { roomPrefabs.Add(loadedPrefab.GetComponent<Room>()); }).Task;

        if (gameObject == null) // not sure if necessary, but some older forum posts mention that this *has* been a necessary precaution when using async-await
        {
            yield break;
        }

        List<Room> spawnedRooms = new List<Room>();
        List<Room> possibleStartRooms = GetPossibleStartRooms();
        List<Room> possiblePathRooms = GetPossiblePathRooms();
        List<Room> possibleEndRooms = GetPossibleEndRooms();

        Room startRoom = SpawnRoom(namePrefix: "Start: ", possibleStartRooms[Random.Range(0, possibleStartRooms.Count)], spawnedRooms);
        Room lastSpawnedRoom = startRoom;

        if (playerInstance != null)
        {
            playerInstance.transform.position = startRoom.transform.position + startRoom.Collider.center + new Vector3(0, 5, 0); // TODO: replace this with an actual spawn point
        }

        for (int i = 0; i < roomsOnPath; i++)
        {
            yield return TryAddNewRoomToSpawnedRoom(namePrefix: string.Format("Path #{0}: ", i), lastSpawnedRoom, possiblePathRooms, spawnedRooms, onFinished: (Room addedRoom) => { lastSpawnedRoom = addedRoom; });
        }

        yield return TryAddNewRoomToSpawnedRoom(namePrefix: "End: ", lastSpawnedRoom, possibleEndRooms, spawnedRooms);

        int previousSpawnedRoomCount = 0;
        int newlySpawnedRoomCount = 0;
        for (int depthIndex = 0; depthIndex < maxBranchDepth; depthIndex++)
        {
            List<Room> possibleBranchRooms = GetPossibleBranchRooms(depthIndex);

            previousSpawnedRoomCount = newlySpawnedRoomCount;
            newlySpawnedRoomCount = spawnedRooms.Count - previousSpawnedRoomCount;

            for (int newRoomIndex = 0; newRoomIndex < newlySpawnedRoomCount; newRoomIndex++)
            {
                Room room = spawnedRooms[previousSpawnedRoomCount + newRoomIndex];

                while (room.HasUnconnectedPassages())
                {
                    yield return TryAddNewRoomToSpawnedRoom(namePrefix: string.Format("Branch (Depth: {0}): ", depthIndex + 1), room, possibleBranchRooms, spawnedRooms);
                }
            }
        }
    }

    private List<Room> GetPossibleStartRooms()
    {
        return roomPrefabs.Where(x => x.Passages.Length == MAX_PASSAGES_ALLOWED_AT_START).ToList();
    }

    private List<Room> GetPossiblePathRooms()
    {
        return roomPrefabs.Where(x => x.Passages.Length >= 2).ToList();
    }

    private List<Room> GetPossibleEndRooms()
    {
        return roomPrefabs.Where(x => x.Passages.Length == MAX_PASSAGES_ALLOWED_AT_END).ToList();
    }

    private List<Room> GetPossibleBranchRooms(int depth)
    {
        if (depth == maxBranchDepth - 1)
        {
            return roomPrefabs.Where(x => x.Passages.Length == 1).ToList(); // TODO: would be nice if the amount of doors randomly tapered off instead
        }

        return roomPrefabs;
    }

    private static IEnumerator TryAddNewRoomToSpawnedRoom(string namePrefix, Room oldRoom, List<Room> prefabsToChooseFrom, List<Room> allSpawnedRooms, System.Action<Room> onFinished = null)
    {
        prefabsToChooseFrom.Shuffle();

        foreach (Room newRoomPrefab in prefabsToChooseFrom)
        {
            Room newRoom = SpawnRoom(namePrefix, newRoomPrefab, allSpawnedRooms);

            bool success = false;
            yield return TryConnectRooms(oldRoom, newRoom, allSpawnedRooms, (bool result) => { success = result; });

            if (success)
            {
                if (onFinished != null)
                {
                    onFinished(newRoom);
                }

                yield break;
            }

            DestroyRoom(newRoom, allSpawnedRooms);
        }

        throw new System.Exception(string.Format("LevelGenerator failed in finding a room able to attach anywhere on {0}!", oldRoom.name));
    }

    private static Room SpawnRoom(string namePrefix, Room roomPrefab, List<Room> spawnedRooms)
    {
        // TODO: would be safer to use Addressables.Instantiate, memory-management wise - but we have to remove LoadAssetsAsync then, if that's possible 

        Room roomInstance = Instantiate(roomPrefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<Room>();
        roomInstance.name = namePrefix + roomInstance.name;

        spawnedRooms.Add(roomInstance);

        return roomInstance;
    }

    private static void DestroyRoom(Room roomInstance, List<Room> spawnedRooms)
    {
        spawnedRooms.Remove(roomInstance);
        Destroy(roomInstance.gameObject);
    }

    private static IEnumerator TryConnectRooms(Room oldRoom, Room newRoom, List<Room> spawnedRooms, System.Action<bool> onFinished)
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

            foreach (Passage newRoomPassage in newRoomPassages)
            {
                if (newRoomPassage.HasConnection)
                {
                    continue;
                }

                CardinalDirection oldPassageDirection = Utils.GetCardinalDirection(oldRoomPassage.transform);
                CardinalDirection newPassageDirection = Utils.GetCardinalDirection(newRoomPassage.transform);
                CardinalDirection desiredDirection = Utils.GetOppositeDirection(oldPassageDirection);

                int attemptsNeeded = System.Enum.GetValues(typeof(CardinalDirection)).Length;
                for (int attemptIndex = 0; attemptIndex < attemptsNeeded; attemptIndex++)
                {
                    if (Utils.GetCardinalDirection(newRoomPassage.transform) == desiredDirection)
                    {
                        break;
                    }

                    if (attemptIndex == attemptsNeeded - 1)
                    {
                        throw new System.Exception(string.Format("{0} failed to find the correct rotation, from {1} to {2}!", newRoom.name, newPassageDirection, desiredDirection));
                    }

                    newRoom.transform.Rotate(0f, 45f, 0f, Space.World); // TODO: calculate instead how many degrees are needed to get from one CardinalDirection to another
                }

                Vector3 newPassageOffset = newRoomPassage.transform.position - newRoom.transform.position;
                newRoom.transform.position = oldRoomPassage.transform.position - newPassageOffset;

                yield return new WaitForSeconds(0.05f);

                if (!TestRoomConnectionForCollisions(oldRoom, newRoom, newRoomPassage, spawnedRooms))
                {
                    oldRoomPassage.HasConnection = true;
                    newRoomPassage.HasConnection = true;
                    onFinished(true);
                    yield break;
                }
            }
        }

        onFinished(false);
    }

    private static bool TestRoomConnectionForCollisions(Room oldRoom, Room newRoom, Passage newRoomPassageUsed, List<Room> spawnedRooms)
    {
        foreach (Room someRoom in spawnedRooms)
        {
            if (someRoom == oldRoom)
            {
                continue;
            }

            if (someRoom == newRoom)
            {
                continue;
            }

            if (newRoom.Collider.bounds.Intersects(someRoom.Collider.bounds))
            {
                Debug.LogFormat("FAIL: {0} was colliding with {1}!", newRoom.name, someRoom.name);
                return true;
            }

            foreach (Passage passage in someRoom.Passages)
            {
                if (passage.HasConnection)
                {
                    continue;
                }

                if (newRoom.Collider.bounds.Intersects(passage.Collider.bounds))
                {
                    Debug.LogFormat("FAIL: {0} was colliding with one of {1}'s passages!", newRoom.name, someRoom.name);
                    return true;
                }
            }

            foreach (Passage passage in newRoom.Passages)
            {
                if (passage.HasConnection)
                {
                    continue;
                }

                if (passage == newRoomPassageUsed)
                {
                    continue;
                }

                if (passage.Collider.bounds.Intersects(someRoom.Collider.bounds))
                {
                    Debug.LogFormat("FAIL: One of {0}'s passages was colliding with {1}!", newRoom.name, someRoom.name);
                    return true;
                }

                foreach (Passage otherRoomPassage in someRoom.Passages)
                {
                    if (otherRoomPassage.HasConnection)
                    {
                        continue;
                    }

                    if (passage.Collider.bounds.Intersects(otherRoomPassage.Collider.bounds))
                    {
                        Debug.LogFormat("FAIL: One of {0}'s passages was colliding with one of {1}'s passages!", newRoom.name, someRoom.name);
                        return true;
                    }
                }
            }
        }

        return false;
    }
}