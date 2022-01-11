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



    private List<Room> spawnedRooms = new List<Room>();

    [SerializeField] private Material mat;
    Texture2D tex;
    const int TEX_SIZE = 150;
    const int TEX_OFFSET = TEX_SIZE / 2;


    private void OnValidate()
    {
        roomsOnPath = Mathf.Max(1, roomsOnPath);
        maxBranchDepth = Mathf.Max(1, maxBranchDepth);
    }

    private void Awake()
    {
        SetRandomSeed();
    }

    private void Start()
    {
        tex = new Texture2D(TEX_SIZE, TEX_SIZE);
        tex.filterMode = FilterMode.Point;

        StartCoroutine(GenerateLevel());
    }

    private void SetRandomSeed()
    {
        int seedToUse = useRandomSeed ? seed : Random.Range(int.MinValue, int.MaxValue); // the point of this is just to expose the seed in the editor, so we can save it if we want

        Debug.LogFormat("SEED: {0}", seedToUse);
        Random.InitState(seedToUse);
    }

    private IEnumerator GenerateLevel()
    {
        yield return Addressables.LoadAssetsAsync<GameObject>(ROOM_PREFABS_ADDRESSABLE_LABEL, (GameObject loadedPrefab) => { roomPrefabs.Add(loadedPrefab.GetComponent<Room>()); }).Task;

        if (gameObject == null) // not sure if necessary, but some older forum posts mention that this *has* been a necessary precaution when using async-await
        {
            yield break;
        }


        foreach (var prefab in roomPrefabs)
        {
            PoolManager.Instance.warmPool(prefab.gameObject, 10);
        }

        List<Room> possibleStartRooms = GetPossibleStartRooms();
        List<Room> possiblePathRooms = GetPossiblePathRooms();
        List<Room> possibleEndRooms = GetPossibleEndRooms();

        Room startRoom = SpawnRoom(possibleStartRooms[Random.Range(0, possibleStartRooms.Count)]);
        OnSuccessfullyPlacedRoom(startRoom, namePrefix: "Start: ");

        Room lastSpawnedRoom = startRoom;

        if (playerInstance != null)
        {
            playerInstance.transform.position = startRoom.transform.position + startRoom.Collider.center + new Vector3(0, 5, 0); // TODO: replace this with an actual spawn point
        }

        CardinalDirection forbiddenDirection = CardinalDirection.None;

        for (int i = 0; i < roomsOnPath; i++)
        {
            yield return TryAddNewRoomToSpawnedRoom(namePrefix: string.Format("Path #{0}: ", i), lastSpawnedRoom, possiblePathRooms, forbiddenDirection, onFinished: (Room addedRoom) =>
            {
                lastSpawnedRoom = addedRoom;

                if (i == 0)
                {
                    foreach (Passage passage in startRoom.Passages)
                    {
                        if (passage.HasConnection)
                        {
                            forbiddenDirection = Utils.GetOppositeDirection(Utils.GetCardinalDirection(passage.transform)); // by forbidding the initial opposite direction, we should be able to prevent the level from curling back in on itself
                            break;
                        }
                    }
                }
            });
        }

        yield return TryAddNewRoomToSpawnedRoom(namePrefix: "End: ", lastSpawnedRoom, possibleEndRooms, forbiddenDirection);

        int spawnedBranchRoomCount = 0;
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
                    yield return TryAddNewRoomToSpawnedRoom(namePrefix: string.Format("BranchRoom #{0}: ", spawnedBranchRoomCount), room, possibleBranchRooms, forbiddenDirection: CardinalDirection.None);
                    spawnedBranchRoomCount++;
                }
            }
        }

        tex.Apply();
        mat.mainTexture = tex;
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

    private IEnumerator TryAddNewRoomToSpawnedRoom(string namePrefix, Room oldRoom, List<Room> prefabsToChooseFrom, CardinalDirection forbiddenDirection, System.Action<Room> onFinished = null)
    {
        prefabsToChooseFrom.Shuffle();

        Debug.LogFormat("========== Trying to add room to {0} ==========", oldRoom.name);

        foreach (Room newRoomPrefab in prefabsToChooseFrom)
        {
            Room newRoom = SpawnRoom(newRoomPrefab);

            bool success = false;
            List<string> errors = null;
            yield return TryConnectRooms(oldRoom, newRoom, forbiddenDirection, (bool result, List<string> errorMessages) =>
            {
                success = result;
                errors = errorMessages;
            });

            if (!success)
            {
                Debug.LogFormat("Failed using {0};", newRoom.name);
                foreach (string message in errors)
                {
                    Debug.Log("\t" + message);
                }

                OnFailedPlacingRoom(newRoom);
                continue;
            }

            if (onFinished != null)
            {
                onFinished(newRoom);
            }

            OnSuccessfullyPlacedRoom(newRoom, namePrefix);
            Debug.Log("========== Success ==========");
            yield break;
        }

        throw new System.Exception(string.Format("LevelGenerator failed in finding a room able to attach anywhere on {0}!", oldRoom.name));
    }

    private Room SpawnRoom(Room roomPrefab)
    {
        // TODO: would be safer to use Addressables.Instantiate, memory-management wise - but we have to remove LoadAssetsAsync then, if that's possible 

        Room roomInstance = PoolManager.Instance.spawnObject(roomPrefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<Room>();// Instantiate(roomPrefab.gameObject, Vector3.zero, Quaternion.identity).GetComponent<Room>();
        roomInstance.transform.SetParent(transform);

        return roomInstance;
    }

    private void OnSuccessfullyPlacedRoom(Room room, string namePrefix)
    {
        room.name = namePrefix + room.name;
        spawnedRooms.Add(room);

        {
            BoundingBox2D bb = BoundingBox2D.GetRoomBoundingBox(room, true);

            Color roomColor = Random.ColorHSV();
            roomColor.a = 1;

            for (int x = 0; x < bb.Dimensions.x; x++)
            {
                for (int y = 0; y < bb.Dimensions.y; y++)
                {
                    Vector2Int pixel = new Vector2Int(TEX_OFFSET + bb.BottomLeftCorner.x + x, TEX_OFFSET + bb.BottomLeftCorner.y + y);

                    if (tex.GetPixel(pixel.x, pixel.y) == Color.yellow)
                    {
                        continue;
                    }

                    tex.SetPixel(pixel.x, pixel.y, roomColor);
                }
            }
        }

        foreach (Passage passage in room.Passages)
        {
            BoundingBox2D bb = BoundingBox2D.GetPassageBoundingBox(passage);

            for (int x = 0; x < bb.Dimensions.x; x++)
            {
                for (int y = 0; y < bb.Dimensions.y; y++)
                {
                    tex.SetPixel(TEX_OFFSET + bb.BottomLeftCorner.x + x, TEX_OFFSET + bb.BottomLeftCorner.y + y, Color.yellow);
                }
            }
        }
    }

    private void OnFailedPlacingRoom(Room room)
    {
        //Destroy(roomInstance.gameObject);
        PoolManager.Instance.releaseObject(room.gameObject);
    }

    private IEnumerator TryConnectRooms(Room oldRoom, Room newRoom, CardinalDirection forbiddenDirection, System.Action<bool, List<string>> onFinished)
    {
        List<string> errorMessages = new List<string>();

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

                yield return new WaitForSeconds(0.01f);

                if (newRoom.Passages.Length > 1 && !DoesRoomHaveOtherPassageNotPointingInDirection(newRoom, newRoomPassage, forbiddenDirection))
                {
                    continue;
                }

                if (IsNewRoomCollidingWithAnything(oldRoom, newRoom, newRoomPassage, out string errorMessage))
                {
                    errorMessages.Add(errorMessage);
                    continue;
                }

                oldRoomPassage.HasConnection = true;
                newRoomPassage.HasConnection = true;
                onFinished(true, null);
                yield break;
            }
        }

        onFinished(false, errorMessages);
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

    private bool IsNewRoomCollidingWithAnything(Room oldRoom, Room newRoom, Passage newRoomPassageUsed, out string errorMessage)
    {
        BoundingBox2D newRoomBoundingBox = BoundingBox2D.GetRoomBoundingBox(newRoom);

        foreach (Room otherRoom in spawnedRooms)
        {
            if (otherRoom == oldRoom)
            {
                continue;
            }

            BoundingBox2D otherRoomBoundingBox = BoundingBox2D.GetRoomBoundingBox(otherRoom);

            if (BoundingBox2D.AreBoxesColliding(newRoomBoundingBox, otherRoomBoundingBox))
            {
                errorMessage = string.Format("Collides with {0} (Pos: {1}, Rot: {2}, BoxBL: {3}, BoxTR: {4})!", otherRoom.name, newRoom.transform.position, newRoom.transform.eulerAngles, newRoomBoundingBox.BottomLeftCorner, newRoomBoundingBox.TopRightCorner);
                return true;
            }

            // if (newRoom.Collider.bounds.Intersects(someRoom.Collider.bounds))
            // {
            //     errorMessage = string.Format("Collides with {0} (Pos: {1}, Rot: {2}, Center: {3}, Size: {4})!", someRoom.name, newRoom.transform.position, newRoom.transform.eulerAngles, newRoom.GetComponent<BoxCollider>().center, newRoom.GetComponent<BoxCollider>().size);
            //     return true;
            // }

            foreach (Passage otherRoomPassage in otherRoom.Passages)
            {
                if (otherRoomPassage.HasConnection)
                {
                    continue;
                }

                BoundingBox2D otherRoomPassageBoundingBox = BoundingBox2D.GetPassageBoundingBox(otherRoomPassage);

                if (BoundingBox2D.AreBoxesColliding(newRoomBoundingBox, otherRoomPassageBoundingBox))
                {
                    errorMessage = string.Format("Collides with {0}'s {1} (Pos: {2}, {3})!", otherRoom.name, otherRoomPassage.name, newRoom.transform.position, newRoom.transform.eulerAngles);
                    return true;
                }

                // if (newRoom.Collider.bounds.Intersects(passage.Collider.bounds))
                // {
                //     errorMessage = string.Format("Collides with {0}'s {1} (Pos: {2}, {3})!", someRoom.name, passage.name, newRoom.transform.position, newRoom.transform.eulerAngles);
                //     return true;
                // }
            }

            foreach (Passage newRoomPassage in newRoom.Passages)
            {
                if (newRoomPassage == newRoomPassageUsed)
                {
                    continue;
                }

                BoundingBox2D newRoomPassageBoundingBox = BoundingBox2D.GetPassageBoundingBox(newRoomPassage);

                if (BoundingBox2D.AreBoxesColliding(newRoomPassageBoundingBox, otherRoomBoundingBox))
                {
                    errorMessage = string.Format("{0} collides with {1}, (Pos: {2}, {3})!", newRoomPassage.name, otherRoom.name, newRoom.transform.position, newRoom.transform.eulerAngles);
                    return true;
                }

                // if (passage.Collider.bounds.Intersects(someRoom.Collider.bounds))
                // {
                //     errorMessage = string.Format("{0} collides with {1}, (Pos: {2}, {3})!", passage.name, someRoom.name, newRoom.transform.position, newRoom.transform.eulerAngles);
                //     return true;
                // }

                foreach (Passage otherRoomPassage in otherRoom.Passages)
                {
                    if (otherRoomPassage.HasConnection)
                    {
                        continue;
                    }

                    BoundingBox2D otherRoomPassageBoundingBox = BoundingBox2D.GetPassageBoundingBox(otherRoomPassage);

                    if (BoundingBox2D.AreBoxesColliding(newRoomPassageBoundingBox, otherRoomPassageBoundingBox))
                    {
                        errorMessage = string.Format("{0} collides with {1} (Pos: {2}, {3})!", newRoomPassage.name, otherRoomPassage.name, newRoom.transform.position, newRoom.transform.eulerAngles);
                        return true;
                    }

                    // if (newRoomPassage.Collider.bounds.Intersects(otherRoomPassage.Collider.bounds))
                    // {
                    //     errorMessage = string.Format("{0} collides with {1} (Pos: {2}, {3})!", newRoomPassage.name, otherRoomPassage.name, newRoom.transform.position, newRoom.transform.eulerAngles);
                    //     return true;
                    // }
                }
            }
        }

        errorMessage = "";
        return false;
    }
}