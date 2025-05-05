using Fusion;
using GorillaLocomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrogmanSkills : NetworkBehaviour
{
    [Header("Ability Parameters")]
    [SerializeField] private float spellCooldown = 5.0f;
    [SerializeField] private float spellRange = 10.0f;
    [SerializeField] private float effectDuration = 2.0f;
    [SerializeField] private float lastSpellUseTime = 10.0f;

    [Header("References")] [Space(7.5f)]
    [SerializeField] private Player player;
    [SerializeField] private NetworkPrefabRef magicProjectilePrefab;
    [SerializeField] private Transform wandTipPoint;

    private NetworkRunner runner;

    public override void Spawned()
    {
        runner = player.PlayerNetworkRunner;
    }

    private void Update()
    {
        if (lastSpellUseTime < spellCooldown)
        {
            lastSpellUseTime += Time.deltaTime;
        }
        else
        {
            //Maybe add a condition to check if player is holding Trigger to "prime" wand
            if (player.CheckIfPrimaryButtonPressed())
            {
                lastSpellUseTime = 0.0f;

                NetworkObject magicProjectile = runner.Spawn(
                    magicProjectilePrefab,
                    wandTipPoint.position,
                    Quaternion.LookRotation(wandTipPoint.forward),
                    Object.InputAuthority);

                var rb = magicProjectile.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.velocity = wandTipPoint.forward * 10.0f;
                }
            }
        }
    }
}
