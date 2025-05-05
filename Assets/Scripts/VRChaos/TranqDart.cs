using Fusion;
using GorillaLocomotion;
using UnityEngine;

public class TranqDart : NetworkBehaviour
{
    [Networked] private PlayerRef owner { get; set; }

    [SerializeField] private Rigidbody rb;
    [SerializeField] private float lifeTime = 2.0f;

    private TickTimer despawnTimer;
    private bool isOwnerEliminated;

    public void Initialize(PlayerRef player, float force, bool isEliminated)
    {
        owner = player;
        rb.velocity = transform.forward * force;
        despawnTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        isOwnerEliminated = isEliminated;
    }

    public override void FixedUpdateNetwork()
    {
        if (despawnTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

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
