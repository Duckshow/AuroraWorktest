using UnityEngine;
using UnityEngine.Assertions;

public class RoomTile : MonoBehaviour
{
    public enum TileType { None, Floor, Wall, Passage }

    [SerializeField] protected TileType type;

    public TileType Type { get { return type; } }

    protected virtual void OnValidate()
    {
        if (!(this is Passage))
        {
            Assert.AreNotEqual(TileType.Passage, type, string.Format("{0} is of the type 'Passage', but isn't using the Passage-script!", transform.name));
        }
    }
}
