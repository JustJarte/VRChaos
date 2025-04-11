using Fusion;
using UnityEngine;
using GorillaLocomotion;

public class PlayerNetworkController : NetworkBehaviour
{
    private Player player;

    public Transform headTransform;
    public Transform leftHand;
    public Transform rightHand;

    public override void Spawned()
    {
        /*if (Object.HasInputAuthority)
        {
            //Enable local inputs
            Camera.main.transform.SetParent(headTransform);
            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.identity;
        }
        else
        {
            //Disable colliders for remote players so they don't block local
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
        }*/

        if (!HasInputAuthority)
        {
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
        }

        player = GetComponent<Player>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority)
            return;

        //GorillaLocomotion runs from FixedUpdate, so it's already active
        //Make sure player physics are syncing properly through NetworkTransform
    }
}
