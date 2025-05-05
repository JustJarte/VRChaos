using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HandPositioningSaver : MonoBehaviour
{
    [SerializeField] private HandPositioningSO handPositioningCollection;
    [SerializeField] private string objectNameToSave;

    private List<HandPositioningSO.HandAndFingerPositioning> handAndFingersPositionsReferenceList = new List<HandPositioningSO.HandAndFingerPositioning>();
    private int totalChildren = 9;
    private int childrenCounter = 0;

    public void SavePositioningSnapshot()
    {
        if (handPositioningCollection == null)
        {
            Debug.LogError("No SO assigned to save to.");
            return;
        }

        //handPositioningCollection.objectToHands.Clear();
        handAndFingersPositionsReferenceList.Clear();
        totalChildren = 9;
        childrenCounter = 0;

        SaveTransformsRecursive(transform, "", handPositioningCollection.objectToHands);
    }

    private void GetTotalChildren(Transform current)
    {
        foreach (Transform child in current)
        {
            totalChildren++;
        }

        Debug.Log(totalChildren);
    }

    private void SaveTransformsRecursive(Transform current, string parentPath, List<HandPositioningSO.ObjectToHandPositioning> list)
    {
        if (childrenCounter < totalChildren)
        {
            childrenCounter++;

            string currentPath = string.IsNullOrEmpty(parentPath) ? current.name : $"{parentPath}/current.name";

            HandPositioningSO.HandAndFingerPositioning handAndFingerPositions = new HandPositioningSO.HandAndFingerPositioning
            {
                boneName = current.name,
                path = currentPath,
                position = current.localPosition,
                rotation = current.localRotation,
                scale = current.localScale
            };

            handAndFingersPositionsReferenceList.Add(handAndFingerPositions);

            foreach (Transform child in current)
            {
                SaveTransformsRecursive(child, currentPath, list);
            }
        }
        else
        {
            list.Add(new HandPositioningSO.ObjectToHandPositioning
            {
                objectName = objectNameToSave,
                handAndFingerPositionings = handAndFingersPositionsReferenceList
            });
        }
    }
}
