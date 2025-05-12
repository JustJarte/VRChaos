using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Only to be used in the Unity Editor, not at Runtime. Allows me to save a list of all the positions and rotations of each finger joint when posed to hold a specific object so that I can then reference those values in the ScriptableObject to animate the hand
// to holding that specific object in-game. Currently there are only two held objects, and one is permanently held by a specific Cryptid (Frogman's wand), the other item is the Tranq Crossbow for Battle Mode. But this can be further utilized for future items.
[ExecuteInEditMode]
public class HandPositioningSaver : MonoBehaviour
{
    [SerializeField] private HandPositioningSO handPositioningCollection;
    [SerializeField] private string objectNameToSave;

    private List<HandPositioningSO.HandAndFingerPositioning> handAndFingersPositionsReferenceList = new List<HandPositioningSO.HandAndFingerPositioning>();
    private int totalChildren = 9;
    private int childrenCounter = 0;

    // Saves a snapshot of the hand object and its child finger joints' transforms and rotations recursively. 
    public void SavePositioningSnapshot()
    {
        if (handPositioningCollection == null)
        {
            Debug.LogError("No SO assigned to save to.");
            return;
        }

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

    // Recursively saves each object's name, path, position, rotation, and scale, then once all objects have been saved, saves it to a List holding all those values and the object's name that they are supposed to be utilized for.
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
