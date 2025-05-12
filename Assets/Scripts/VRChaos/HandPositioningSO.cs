using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ScriptableObject that defines an object type that saves object name, path, position, rotation, and scale, and also defines a larger-scale object that holds a List of the previous Type and the object it's connected to. That List can then be accessed at runtime via
// the ScriptableObject to get those values as needed.
[CreateAssetMenu(menuName = "VRChaos/Hand Positions for Held Objects")]
public class HandPositioningSO : ScriptableObject
{
    [System.Serializable]
    public class HandAndFingerPositioning
    {
        public string boneName;
        public string path;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    [System.Serializable]
    public class ObjectToHandPositioning
    {
        public string objectName;
        public List<HandAndFingerPositioning> handAndFingerPositionings = new List<HandAndFingerPositioning>();
    }

    public List<ObjectToHandPositioning> objectToHands = new List<ObjectToHandPositioning>();
}
