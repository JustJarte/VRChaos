using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HandPositioningSaver))]
public class HandPositioningSaverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HandPositioningSaver saver = (HandPositioningSaver)target;

        if (GUILayout.Button("Save Transforms to ScriptableObject"))
        {
            saver.SavePositioningSnapshot();
        }
    }
}
