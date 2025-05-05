using Fusion;
using UnityEngine;
using GorillaLocomotion;

public class PlayerNetworkController : NetworkBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            playerCamera.enabled = true;
            audioListener.enabled = true;

            if (playerCamera.tag != "MainCamera")
            {
                playerCamera.tag = "MainCamera";
            }
        }
        else
        {
            playerCamera.enabled = false;
            audioListener.enabled = false;
        }
    }
}
