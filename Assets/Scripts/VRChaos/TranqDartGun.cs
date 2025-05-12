using Fusion;
using GorillaLocomotion;
using UnityEngine;

// Handles the Tranq Dart Crossbow Gun logic. Receives information from the Pull Piece script to know when the Crossbow is primed and ready to be fired, and once it is, the Player can hit the left or right Trigger (based
// on their primary hand preference, set in options) to fire a Tranq Dart.
public class TranqDartGun : NetworkBehaviour
{
    [SerializeField] private NetworkPrefabRef dartPrefab;
    [SerializeField] private OptionSettingsSO options;
    [SerializeField] private Player player;
    [SerializeField] private Transform firePoint;
    [SerializeField] private TranqDartGunPullPiece pullAnchor;
    [SerializeField] private float fireForce = 20.0f;

    // If it's the local player, based on their primary hand preference, which defaults to Right, if that player presses in the Trigger button for that hand, and the Pull Piece is Primed, we fire an instance of
    // the Tranq Dart from the fire point and then we reset the Pull Piece to prepare to start priming again.
    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;

        if (options.playerRightHanded)
        {
            if (player.CheckIfRightTriggerPressed())
            {
                if (pullAnchor.IsPrimed)
                {
                    FireProjectile();

                    pullAnchor.ReleaseAnchor();
                }
            }
        }
        else
        {
            if (player.CheckIfLeftTriggerPressed())
            {
                if (pullAnchor.IsPrimed)
                {
                    FireProjectile();

                    pullAnchor.ReleaseAnchor();
                }
            }
        }
    }

    // Fires the Tranq Dart projectile from the fire point Transform, and then Initializes the Tranq Dart in its own script upon being spawned. 
    private void FireProjectile()
    {
        Runner.Spawn(dartPrefab, firePoint.position, firePoint.rotation, Object.InputAuthority,
            (runner, obj) =>
            {
                var dart = obj.GetComponent<TranqDart>();
                dart.Initialize(Object.InputAuthority, fireForce, player.IsEliminated);
            });
    }
}
