using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Passage : RoomTile
{
    [SerializeField, HideInInspector] private new BoxCollider collider;

    public BoxCollider Collider { get { return collider; } }

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

        CardinalDirection facingDirection = Utils.GetCardinalDirection(GetPivot(), Space.Self);

        collider.size = new Vector3Int(Room.MIN_ROOM_WIDTH, Room.DEFAULT_ROOM_HEIGHT, Room.MIN_ROOM_WIDTH);
        collider.center = GetPivot().localPosition + Utils.CardinalDirectionToVector3(facingDirection);
    }
}
