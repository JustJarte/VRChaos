using Fusion;
using GorillaLocomotion;
using UnityEngine;

// Controls the Alien's ability in game play. 
public class AlienSkills : NetworkBehaviour
{
    [Header("Ability Parameters and References")]
    [SerializeField] private NetworkPrefabRef beaconPrefab;
    [SerializeField] private float beaconCooldown = 5.0f;
    [SerializeField] private float lastBeaconDropTime = 10.0f;

    [Header("Player Controller Reference")] [Space(7.5f)]
    [SerializeField] private Player player;

    private bool justRecalled = false;
    private NetworkObject activeBeacon;

    public override void Spawned() { }

    // We monitor the logic to control the Alien's Beacon ability, which actually has two built in functionalities. We check if the Player is pressing the primary button, and if they are and they aren't currently on cooldown 
    // from a previous spawn, and are grounded, spawn a Beacon object where they are and initialize it. Then, if there is a current Beacon deployed, we can check if the Player is pressing the secondary button, and if they are, we 
    // teleport the player back to that Beacon's location and then despawn the Beacon.
    private void Update()
    {
        if (justRecalled)
        {
            justRecalled = false;
        }
        else if (player.RigidbodyMovement != Vector3.zero)
        {
            transform.position = transform.position + player.RigidbodyMovement;
        }

        if (activeBeacon != null)
        {
            if (player.CheckIfSecondaryButtonPressed())
            {
                transform.position = new Vector3(activeBeacon.transform.position.x, transform.position.y, activeBeacon.transform.position.z);

                Debug.Log("Recalled to beacon!");

                Runner.Despawn(activeBeacon);
                activeBeacon = null;

                justRecalled = true;
            }
        }
        else
        {
            if (player.CheckIfSecondaryButtonPressed())
            {
                Debug.Log("No active beacon to recall!");

                player.SendHapticImpulse(player.leftHandDevice, 0.3f, 0.15f);
                player.SendHapticImpulse(player.rightHandDevice, 0.3f, 0.15f);
            }
        }

        if (lastBeaconDropTime < beaconCooldown)
        {
            lastBeaconDropTime += Time.deltaTime;
        }
        else
        {
            if (player.CheckIfGrounded() && player.CheckIfPrimaryButtonPressed())
            {
                lastBeaconDropTime = 0.0f;
                TryDropAlienBeacon();
            }
        }
    }

    // Handles creating an Alien Beacon. Destroys any existing ones in the map if utilized again, so that there is only 1 active Beacon at a time. 
    private void TryDropAlienBeacon()
    {
        if (activeBeacon != null)
        {
            Runner.Despawn(activeBeacon);
            activeBeacon = null;
        }

        var spawnedObject = Runner.Spawn(
            beaconPrefab,
            new Vector3(transform.position.x, 0.5f, transform.position.z),
            Quaternion.identity,
            inputAuthority: Object.InputAuthority
            );

        activeBeacon = spawnedObject;

        AlienBeacon beaconObject = activeBeacon.GetComponent<AlienBeacon>();

        if (beaconObject != null)
        {
            beaconObject.owner = player;
            beaconObject.skillTracker = this;
        }
    }

    // Notifies the player with haptic feedback that their Beacon was destroyed by an enemy player.
    public void NotifyBeaconDestroyed()
    {
        activeBeacon = null;

        Debug.Log("Your beacon was destroyed!");

        player.SendHapticImpulse(player.leftHandDevice, 0.6f, 0.15f);
        player.SendHapticImpulse(player.rightHandDevice, 0.6f, 0.15f);
    }
}
