using UnityEngine;
using UnityEngine.XR;
using System;

public class VRModeManager : MonoBehaviour
{
    public static bool IsRealVR => XRSettings.isDeviceActive;
    public static bool SimulateVR = true;

    public static bool UseGorillaLocomotion => IsRealVR && !SimulateVR;

    public static event Action OnModeChanged;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SimulateVR = !SimulateVR;
            Debug.Log("Toggled VR Sim Mode: " + SimulateVR);

            OnModeChanged?.Invoke();
        }
    }
}
