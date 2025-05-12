using GorillaLocomotion;
using UnityEngine;

// Controls the Bigfoot's ability in game play. 
public class BigfootSkills : MonoBehaviour
{
    [Header("Ability Parameters")]
    [SerializeField] private float slamCooldown = 5.0f;
    [SerializeField] private float lastSlamTime = 10.0f;
    [SerializeField] private float slamRadius = 5.0f;
    [SerializeField] private float slamForce = 10.0f;
    [SerializeField] private LayerMask affectablePlayerLayer;

    [Header("References")] [Space(7.5f)]
    [SerializeField] private Player player;

    // If the player isn't on cooldown from the last use of the ability, we check if they are grounded and if they are holding the Trigger button on both controllers, and if so, we then check if
    // both hands are touching the ground, and if so, we create a "Slam" effect that has a defined range and force that knocks enemy players away with force added to their rigidbodies.
    private void Update()
    {
        if (lastSlamTime < slamCooldown)
        {
            lastSlamTime += Time.deltaTime;
        }
        else
        {
            if (player.CheckIfGrounded() && player.CheckIfPlayerHoldingTriggers())
            {
                bool bothHandsSlammedDown = player.IsHandTouching(true) && player.IsHandTouching(false);

                if (bothHandsSlammedDown)
                {
                    Debug.Log("Officially using slam ability!");

                    lastSlamTime = 0.0f;

                    Vector3 origin = transform.position;

                    Collider[] hitColliders = Physics.OverlapSphere(origin, slamRadius, affectablePlayerLayer);

                    foreach (var hit in hitColliders)
                    {
                        if (hit.attachedRigidbody && hit.gameObject != gameObject)
                        {
                            Vector3 direction = (hit.transform.position - origin).normalized;
                            hit.attachedRigidbody.AddForce(direction * slamForce, ForceMode.Impulse);
                        }
                    }
                }
            }
        }
    }
}
