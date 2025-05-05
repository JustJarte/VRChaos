using Fusion;
using GorillaLocomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlienSkills : NetworkBehaviour
{
    [Header("Ability Parameters and References")]
    [SerializeField] private NetworkPrefabRef beaconPrefab;
    [SerializeField] private float beaconCooldown = 5.0f;
    [SerializeField] private float lastBeaconDropTime = 10.0f;

    [Header("Player Controller Reference")] [Space(7.5f)]
    [SerializeField] private Player player;

    private bool justRecalled = false;
    private NetworkRunner runner;
    private NetworkObject activeBeacon;

    public override void Spawned()
    {
        runner = player.PlayerNetworkRunner;
    }

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

                runner.Despawn(activeBeacon);
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

    private void TryDropAlienBeacon()
    {
        if (activeBeacon != null)
        {
            runner.Despawn(activeBeacon);
            activeBeacon = null;
        }

        var spawnedObject = runner.Spawn(
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

    public void NotifyBeaconDestroyed()
    {
        activeBeacon = null;

        Debug.Log("Your beacon was destroyed!");

        player.SendHapticImpulse(player.leftHandDevice, 0.6f, 0.15f);
        player.SendHapticImpulse(player.rightHandDevice, 0.6f, 0.15f);
    }
}
