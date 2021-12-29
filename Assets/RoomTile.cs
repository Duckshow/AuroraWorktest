using UnityEngine;

public class RoomTile : MonoBehaviour
{
    public enum TileType { None, Floor, Wall, Passage }

    [SerializeField] private TileType type;

    public TileType Type { get { return type; } }
}
