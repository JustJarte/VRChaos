using Fusion.XR.Shared.Rig;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPreferenceControl : MonoBehaviour
{
    public bool isRightHanded = true;

    public void SetRightHanded(bool value)
    {
        isRightHanded = value;
    }
}
