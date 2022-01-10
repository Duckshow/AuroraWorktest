using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Passage : RoomTile
{
    [SerializeField, HideInInspector] private new BoxCollider collider;

    public BoxCollider Collider { get { return collider; } }

    public bool HasConnection;

    protected override void OnValidate()
    {
        base.OnValidate();

        type = TileType.Passage;
        UpdateCollider();
    }

    public void UpdateCollider()
    {
        if (collider == null)
        {
            collider = GetComponent<BoxCollider>();
        }

        const float TOLERANCE = 0.99f;
        collider.size = new Vector3(Room.MIN_ROOM_WIDTH, Room.DEFAULT_ROOM_HEIGHT, Room.MIN_ROOM_WIDTH) * TOLERANCE;
        collider.center = new Vector3(0f, collider.size.y / 2, collider.size.z / 2);
    }
}
