using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    private const string ROOM_PREFABS_ADDRESSABLE_LABEL = "Rooms";

    [SerializeField] private int seed = 92985421;
    [SerializeField] private int roomsOnPath;

    private List<Room> roomPrefabs = new List<Room>();

    private void Start()
    {
        Random.InitState(seed);
        GenerateLevel();
    }

    private async void GenerateLevel()
    {
        await Addressables.LoadAssetsAsync<GameObject>(ROOM_PREFABS_ADDRESSABLE_LABEL, (GameObject loadedPrefab) => { roomPrefabs.Add(loadedPrefab.GetComponent<Room>()); }).Task;

        if (gameObject == null) // not sure if necessary, but some older forum posts mention that this *has* been a necessary precaution when using async-await
        {
            return;
        }

        List<Room> roomInstances = new List<Room>();

        Room startRoom = SpawnRoom(roomPrefabs[Random.Range(0, roomPrefabs.Count)], roomInstances);
        Debug.LogFormat("Added starting room! ({0})", startRoom.name);

        Room previousRoom = startRoom;

        List<Room> shufflableRoomPrefabs = new List<Room>(roomPrefabs);
        for (int i = 0; i < roomsOnPath; i++)
        {
            TryAddNewRoomToPath(ref previousRoom, shufflableRoomPrefabs, roomInstances, minPassageCount: 2);
            Debug.LogFormat("Added room #{0}!", i);
        }

        TryAddNewRoomToPath(ref previousRoom, shufflableRoomPrefabs, roomInstances);
        Debug.LogFormat("Added end room! ({0})", previousRoom.name);

        /*
        5. Find all remaining doors
        6. Loop over found doors
            6.1. Loop X times, decreasing the spawn chance for doors every time
                6.1.1. Spawn a random room
                6.1.2. Pick a random door on the current room and a random door on the new room
                6.1.3. Rotate the new room so the doors are facing eachother
                6.1.4. Position the new room so that the doors are overlapping
                6.1.5. Check for collision with any spawned room
                    6.1.5.1. If true, try again with a new rotation
                        6.1.5.1.1. If all rotations fail, set the room as temporarily ignored and go back to 2.1.
                            6.1.5.1.1.1. If all prefabs fail, throw an exception.
        */
    }

    private static bool TryAddNewRoomToPath(ref Room previousRoom, List<Room> shuffledRoomPrefabs, List<Room> spawnedRooms, int minPassageCount = 1)
    {
        shuffledRoomPrefabs.Shuffle();

        foreach (Room newRoomPrefab in shuffledRoomPrefabs)
        {
            if (newRoomPrefab.Passages.Length < minPassageCount)
            {
                continue;
            }

            Room newRoom = SpawnRoom(newRoomPrefab, spawnedRooms);
            Debug.Log("Trying to spawn " + newRoom.name);

            if (TryConnectRooms(previousRoom, newRoom, spawnedRooms))
            {
                Debug.Log("Success!");
                previousRoom = newRoom;
                return true;
            }

            DestroyRoom(newRoom, spawnedRooms);
        }

        throw new System.Exception(string.Format("LevelGenerator failed in finding a room able to attach anywhere on {0}!", previousRoom.name));
    }

    private static Room SpawnRoom(Room roomPrefab, List<Room> roomInstances)
    {

        // TODO: would be safer to use Addressables.Instantiate, memory-management wise - but we have to remove LoadAssetsAsync then, if that's possible 

        Room roomInstance = Instantiate(roomPrefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<Room>();
        roomInstances.Add(roomInstance);

        return roomInstance;
    }

    private static void DestroyRoom(Room roomInstance, List<Room> roomInstances)
    {
        roomInstances.Remove(roomInstance);
        //Addressables.ReleaseInstance(roomInstance.gameObject);
        Destroy(roomInstance.gameObject);
    }

    private static bool TryConnectRooms(Room oldRoom, Room newRoom, List<Room> spawnedRooms)
    {
        List<RoomTile> oldRoomPassages = new List<RoomTile>(oldRoom.Passages);
        oldRoomPassages.Shuffle();

        List<RoomTile> newRoomPassages = new List<RoomTile>(newRoom.Passages);
        newRoomPassages.Shuffle();

        foreach (RoomTile oldRoomPassage in oldRoomPassages)
        {
            foreach (RoomTile newRoomPassage in newRoomPassages)
            {
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

                if (!IsRoomCollidingWithAnyOther(newRoom, spawnedRooms, roomToIgnore: oldRoom))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsRoomCollidingWithAnyOther(Room room, List<Room> roomInstances, Room roomToIgnore)
    {
        foreach (Room otherRoom in roomInstances)
        {
            if (otherRoom == room)
            {
                continue;
            }

            if (otherRoom == roomToIgnore)
            {
                continue;
            }

            if (room.Collider.bounds.Intersects(otherRoom.Collider.bounds))
            {
                return true;
            }

            foreach (Passage passage in otherRoom.Passages)
            {
                if (room.Collider.bounds.Intersects(passage.Collider.bounds))
                {
                    return true;
                }
            }

            foreach (Passage passage in room.Passages)
            {
                if (passage.Collider.bounds.Intersects(otherRoom.Collider.bounds))
                {
                    return true;
                }

                foreach (Passage otherRoomPassage in otherRoom.Passages)
                {
                    if (passage.Collider.bounds.Intersects(otherRoomPassage.Collider.bounds))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}