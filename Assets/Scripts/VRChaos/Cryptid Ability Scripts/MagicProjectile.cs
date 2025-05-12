using Fusion;
using GorillaLocomotion;
using UnityEngine;

// Frogman's magic projectile class. Just defines and controls his projectile. Once spawned, travels for a certain amount of time forward, if it hits any Player, it applies the magic
// effect and then gets destroyed.
public class MagicProjectile : NetworkBehaviour
{
    public float projectileLifetime = 5.0f;
    public float effectDuration = 2.0f;

    private void Start()
    {
        Destroy(gameObject, projectileLifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Player hitPlayer = GetComponent<Player>();

        if (hitPlayer != null)
        {
            if (hitPlayer.Object.InputAuthority == Object.InputAuthority)
            {
                return;
            }

            hitPlayer.ApplyMagicEffect(effectDuration);
        }

        if (HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}
