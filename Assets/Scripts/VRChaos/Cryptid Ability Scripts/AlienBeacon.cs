using UnityEngine;
using GorillaLocomotion;
using Fusion;

// Controls the status of the Alien's Beacon ability. If a Player attacks the beacon with their hand, OnTriggerEnter occurs and then checks to make sure it is a Player hand, and does an extra check to make sure it's not its owner's hands, and
// if not, despawn the Beacon from the game and notify the Alien player.
public class AlienBeacon : NetworkBehaviour
{
    [HideInInspector] public Player owner;
    [HideInInspector] public AlienSkills skillTracker;

    private void OnTriggerEnter(Collider other)
    {
        foreach (var hand in owner.playerHands)
        {
            if (other.gameObject.layer == 9)
            {
                if (other.gameObject == hand)
                {
                    return;
                }
                else
                {
                    if (skillTracker != null)
                    {
                        skillTracker.NotifyBeaconDestroyed();
                    }

                    Runner.Despawn(Object);

                    return;
                }
            }
        }
    }
}
