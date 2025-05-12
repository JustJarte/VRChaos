using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine;

// Not currently in-use, but was made for extending the options menu capability for a public release. Currenlty I am just saving option settings directly to the ScriptableObject, but
// in the future, such classes as these could be utilized to make modifying and extensions easier. This one would allow the user to recenter their XR devices to the current position
// and orientation of the HMD. 
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
