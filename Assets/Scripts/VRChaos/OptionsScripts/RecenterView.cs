using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine;

public class RecenterView : MonoBehaviour
{
    public void Recenter()
    {
        List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetInstances(inputSubsystems);

        foreach (var subsystem in inputSubsystems)
        {
            subsystem.TryRecenter();
        }
    }
}
