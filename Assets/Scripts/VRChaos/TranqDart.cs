using Fusion;
using GorillaLocomotion;
using UnityEngine;

// Script that handles the Tranq Dart's logic. A Tranq Dart is a projectile that can be shot from the Tranquilizer Crossbow in Battle Mode only. Once fired, the Tranq Dart is Initialized so that it knows its
// owner, knows to start traveling forward, and knows how long it has for a lifespan before being despawned. Additionally, it also tracks if the player firing the Tranq Dart is Eliminated or not, as the
// Tranq Dart has a dual function based on that condition: if not Eliminated, it inflicts damage to the hit player and removes a life, if they are Eliminated, it instead inflicts a slowing status on the hit player.
public class TranqDart : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float lifeTime = 2.0f;

    [Networked] private PlayerRef owner { get; set; }

    private TickTimer despawnTimer;
    private bool isOwnerEliminated;

    // Initializes this object to know its owner, force to start traveling, when to despawn, and if the owner is Eliminated.
    public void Initialize(PlayerRef player, float force, bool isEliminated)
    {
        owner = player;
        rb.velocity = transform.forward * force;
        despawnTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        isOwnerEliminated = isEliminated;
    }

    // Tracks the despawn TickTimer to know when to have the active Runner despawn this object.
    public override void FixedUpdateNetwork()
    {
        if (despawnTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    // If the Tranq Dart hits another Player while active, either slow that enemy Player or take a life from them in Battle Mode, THEN despawn this Tranq Dart object immediately.
    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;

        var hitPlayer = other.GetComponentInParent<Player>();

        if (hitPlayer != null && hitPlayer.Object.InputAuthority != owner)
        {
            if (isOwnerEliminated)
            {
                hitPlayer.IsSlowed = true;
            }
            else
            {
                hitPlayer.RPC_TakeHit(owner);
            }

            Runner.Despawn(Object);
        }
    }
}
