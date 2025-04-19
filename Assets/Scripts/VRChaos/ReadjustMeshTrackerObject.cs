using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadjustMeshTrackerObject : MonoBehaviour
{
    public bool isLeftHand = false;

    public void ResetBackToZero()
    {
        transform.rotation = Quaternion.identity;
    }

    public void ResetAfterRigRevealed()
    {
        //Vector3 targetDirection = new Vector3(-0.5f, 0.0f, -1f);
        //Quaternion rotation = Quaternion.LookRotation(targetDirection, Vector3.up);

        if (isLeftHand)
        {
            transform.localRotation = new Quaternion(-0.5f, -0.5f, -0.5f, 0.5f);

        }
        else
        {
            //transform.rotation = new Quaternion(-0.5f, 0.0f, -0.5f, 0.5f);
            transform.localRotation = Quaternion.Euler(new Vector3(-41.531f, -123.781f, 25.231f));
        }
    }
}
