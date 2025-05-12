using UnityEngine;

// Serializable class that acts as an object of spawn information to be read at runtime. Holds the name of the spawn position, a Color for preview in Editor mode, a position and a rotation, and can be marked as Visible or not in the Editor.
[System.Serializable]
public class SpawnLocationInfo
{
    public string positionName;
    public Color positionColor;
    public Vector3 position;
    public Quaternion rotation;
    public bool isVisible = true;
}
