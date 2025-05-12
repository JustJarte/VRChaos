using Fusion;
using GorillaLocomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The basic template of the Camera pickup for Decryptid Mode. When the Camera is picked up, it initializes and sets its owner, and then gives the Player a target to hunt in that mode. Additionally also handles taking the snapshots and registers if the target
// player to hunt is in the shot.
public class DecryptidCamera : MonoBehaviour
{
    [SerializeField] private Camera decryptidCamera;
    [SerializeField] private RenderTexture cameraRender;
    [SerializeField] private LayerMask detectionLayers;
    [SerializeField] private Transform snapshotOrigin;
    [SerializeField] private float snapshotRange = 50.0f;
    [SerializeField] private float snapshotFOV = 60.0f;

    private Player owner;
    private PlayerRef targetPlayer;

    // Set player's owner and then set their target to hunt for in-game.
    public void InitializeCameraPickup(Player ownerPlayer)
    {
        owner = ownerPlayer;
        targetPlayer = DecryptidModeManager.Instance.GetTargetFor(owner.Object.InputAuthority);
    }

    // When the Player tries to take a picture, call this method to capture colliders in the Physics Sphere and if the Player is the target to hunt, perform the next action.
    public void TryTakeSnapshot()
    {
        var visibleTargets = Physics.OverlapSphere(snapshotOrigin.position, snapshotRange, detectionLayers);

        foreach (var collider in visibleTargets)
        {
            var target = collider.GetComponentInParent<Player>();

            if (target != null && target.Object.InputAuthority == targetPlayer)
            {
                Vector3 dir = (target.transform.position - snapshotOrigin.position).normalized;
                float angle = Vector3.Angle(snapshotOrigin.forward, dir);

                if (angle < snapshotFOV / 2.0f)
                {
                    Debug.Log("Target is in the photo!");

                    return;
                }
            }
        }

        Debug.Log("Target is not in the photo!");
    }
}
