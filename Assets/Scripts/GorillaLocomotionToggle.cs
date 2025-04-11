using GorillaLocomotion;
using UnityEngine;
using Fusion.Addons.Physics;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterController))]
public class GorillaLocomotionToggle : MonoBehaviour
{
    private Player player;
    private Rigidbody rb;
    private CharacterController characterController;
    private NetworkRigidbody3D netRB3D; 

    private void Awake()
    {
        player = GetComponent<Player>();
        rb = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();
        netRB3D = GetComponent<NetworkRigidbody3D>();
    }

    private void OnEnable()
    {
        VRModeManager.OnModeChanged += ApplyLocomotionMode;
        ApplyLocomotionMode();
    }

    private void OnDisable()
    {
        VRModeManager.OnModeChanged -= ApplyLocomotionMode;
    }

    private void ApplyLocomotionMode()
    {
        bool useGorillaLoco = VRModeManager.UseGorillaLocomotion;

        if (player != null)
        {
            player.enabled = useGorillaLoco;
        }

        if (rb != null)
        {
            rb.isKinematic = !useGorillaLoco;
            rb.useGravity = useGorillaLoco;
        }

        if (characterController != null)
        {
            characterController.enabled = !useGorillaLoco;
        }

        if (netRB3D != null)
        {
            netRB3D.enabled = useGorillaLoco;
        }

        Debug.Log($"Switched to {(useGorillaLoco ? "VR Gorilla Mode" : "Desktop WASD Mode")}");
    }
}
