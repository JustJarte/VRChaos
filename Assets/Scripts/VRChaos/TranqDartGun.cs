using Fusion;
using GorillaLocomotion;
using UnityEngine;

public class TranqDartGun : NetworkBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private NetworkPrefabRef dartPrefab;
    [SerializeField] private TranqDartGunPullPiece pullAnchor;
    [SerializeField] private float fireForce = 20.0f;
    [SerializeField] private Player player;
    [SerializeField] private OptionSettingsSO options;

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
