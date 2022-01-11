using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

[CustomEditor(typeof(RoomTile), editorForChildClasses: true)]
public class RoomTileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10f);

        RoomTile roomTile = (RoomTile)target;
        EditorGUILayout.LabelField("Facing: " + Utils.GetCardinalDirection(roomTile.transform).ToString());

        if (GUILayout.Button("Rotate CW")) { RotateTile(roomTile, 45f); }
        if (GUILayout.Button("Rotate CCW")) { RotateTile(roomTile, -45f); }

        RoundAndClampLocalPosition(roomTile);
    }

    private static void RoundAndClampLocalPosition(RoomTile roomTile)
    {
        Vector3 localPos = roomTile.transform.localPosition;

        roomTile.transform.localPosition = new Vector3(
            Mathf.Max(0f, Mathf.Floor(localPos.x)),
            Mathf.Max(0f, Mathf.Floor(localPos.y)),
            Mathf.Max(0f, Mathf.Floor(localPos.z))
        );
    }

    private static void RotateTile(RoomTile roomTile, float degrees)
    {
        roomTile.transform.Rotate(new Vector3(0f, degrees, 0f));
        MarkPrefabDirty();
    }

    private static void MarkPrefabDirty()
    {
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            EditorSceneManager.MarkSceneDirty(prefabStage.scene);
        }
    }
}