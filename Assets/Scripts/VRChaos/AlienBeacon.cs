using UnityEngine;
using GorillaLocomotion;
using Fusion;

public class AlienBeacon : NetworkBehaviour
{
    [HideInInspector] public Player owner;

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
                    if (owner != null)
                    {
                        owner.NotifyBeaconDestroyed();
                    }

                    Runner.Despawn(Object);

                    return;
                }
            }
        }
    }
}
