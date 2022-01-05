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

    public Transform GetPivot()
    {
        Transform pivot = transform.GetChild(0);
        Assert.AreEqual("Pivot", pivot.tag, string.Format("Failed to find {0}'s Pivot-transform! Expected a single child transform with the Pivot-tag!", gameObject.name));
        return pivot;
    }
}
