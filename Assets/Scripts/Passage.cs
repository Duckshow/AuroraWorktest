using UnityEngine;

public class Passage : RoomTile
{
    public bool HasConnection;

    protected override void OnValidate()
    {
        base.OnValidate();
        type = TileType.Passage;
    }
}
