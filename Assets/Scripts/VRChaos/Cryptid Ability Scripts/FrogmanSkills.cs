using Fusion;
using GorillaLocomotion;
using UnityEngine;

// Controls the Frogman's ability in game play. 
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

    public override void Spawned() { }

    // Frogman is known for carrying a wand, so naturally his active ability allows him to cast a spell. Currently only one, but the idea was to give him 2 or 3 if further developed and defined. We make sure the player isn't on
    // cooldown, and if not, we check if the primary button has been pressed, and if so, we spawn a magic projectile and send it shooting forward.
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

                NetworkObject magicProjectile = Runner.Spawn(
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
