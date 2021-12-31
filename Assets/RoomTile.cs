using UnityEngine;
using UnityEngine.Assertions;

public class RoomTile : MonoBehaviour
{
    public enum TileType { None, Floor, Wall, Passage }

    [SerializeField] private TileType type;

    public TileType Type { get { return type; } }

    public CardinalDirection GetCardinalDirection()
    {
        float rot = GetPivot().eulerAngles.y;

        if (rot < 22.5f) { return CardinalDirection.North; }
        else if (rot < 67.5f) { return CardinalDirection.NorthEast; }
        else if (rot < 112.5f) { return CardinalDirection.East; }
        else if (rot < 157.5f) { return CardinalDirection.SouthEast; }
        else if (rot < 202.5f) { return CardinalDirection.South; }
        else if (rot < 247.5f) { return CardinalDirection.SouthWest; }
        else if (rot < 292.5) { return CardinalDirection.West; }
        else if (rot < 337.5f) { return CardinalDirection.NorthWest; }
        else { return CardinalDirection.North; }
    }

    public Transform GetPivot()
    {
        Transform pivot = transform.GetChild(0);
        Assert.AreEqual("Pivot", pivot.tag, string.Format("Failed to find {0}'s Pivot-transform! Expected a single child transform with the Pivot-tag!", gameObject.name));
        return pivot;
    }

    public Vector2Int GetCoordinatesInRoom()
    {
        return new Vector2Int(
            Mathf.FloorToInt(transform.localPosition.x),
            Mathf.FloorToInt(transform.localPosition.z)
        );
    }
}
