using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
