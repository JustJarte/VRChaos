using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SpawnLocationsGizmoDrawer
{
    static SpawnLocationsGizmoDrawer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (Selection.activeObject is SpawnLocationsSO spawnLocationsSO)
        {
            for (int i = 0; i < spawnLocationsSO.spawnLocations.Count; i++)
            {
                var entry = spawnLocationsSO.spawnLocations[i];
                if (entry == null || !entry.isVisible) continue;

                Handles.color = entry.positionColor;

                Handles.SphereHandleCap(0, entry.position, entry.rotation, 2.5f, EventType.Repaint);

                EditorGUI.BeginChangeCheck();

                Vector3 newPos = Handles.PositionHandle(entry.position, entry.rotation);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(spawnLocationsSO, "Move Position Entry");
                    entry.position = newPos;
                    EditorUtility.SetDirty(spawnLocationsSO);
                }

                Handles.Label(entry.position + Vector3.down * 0.5f, entry.positionName);
            }
        }
    }
}
