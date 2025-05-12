using GorillaLocomotion;
using UnityEngine;

// Controls the Mothman's ability in game play. 
public class MothmanSkills : MonoBehaviour
{
    [Header("Ability Parameters")]
    [SerializeField] private int currentWingFlapsRemaining = 5;
    [SerializeField] private int maxWingFlaps = 5;
    [SerializeField] private float flapCooldown = 0.25f;
    [SerializeField] private float lastFlapTime = 0.0f;
    [SerializeField] private float glideFallSpeed = -2.0f;

    [Header("References")] [Space(7.5f)]
    [SerializeField] private Player player;
    [SerializeField] private Rigidbody playerRigidbody;

    private bool hasRecentlyFlapped = false;

    // Mothman has the ability to boost themself up to 3 times while in the air, giving them a slight boost upward and forward momentum. Once all 3 uses of the boost have been used, the Mothman can further slow their descent
    // by holding their glide, or can lower their arms to stop gliding and start plummeting to the ground faster. Boosts are restored once the player hits the ground; will likely also add a slight cooldown so they can't
    // be spammed repeatedly. 
    private void Update()
    {
        if (player.CheckIfGrounded() && currentWingFlapsRemaining < maxWingFlaps)
        {
            currentWingFlapsRemaining = maxWingFlaps;
        }

        var currentLeftLocal = player.headCollider.transform.InverseTransformPoint(player.leftHandTransform.position);
        var currentRightLocal = player.headCollider.transform.InverseTransformPoint(player.rightHandTransform.position);

        float leftDownSpeed = player.previousLeftHandLocalPos.y - currentLeftLocal.y;
        float rightDownSpeed = player.previousRightHandLocalPos.y - currentRightLocal.y;

        bool bothHandsFlapping = leftDownSpeed > 0.0001f && rightDownSpeed > 0.0001f;

        bool handsRaised = false;

        if (lastFlapTime < flapCooldown)
        {
            lastFlapTime += Time.deltaTime;
        }
        else
        {
            if (bothHandsFlapping && !hasRecentlyFlapped && currentWingFlapsRemaining > 0)
            {
                Vector3 headForward = player.headCollider.transform.forward;
                Vector3 flapDirection = (Vector3.up * 1.2f + headForward * 0.8f).normalized;

                playerRigidbody.velocity = flapDirection * 10.0f;

                currentWingFlapsRemaining--;
                lastFlapTime = 0.0f;
                hasRecentlyFlapped = true;
            }
            else if (!bothHandsFlapping)
            {
                hasRecentlyFlapped = false;
            }
        }

        if (!handsRaised && currentWingFlapsRemaining < maxWingFlaps)
        {
            if (playerRigidbody.velocity.y < glideFallSpeed)
            {
                playerRigidbody.velocity = new Vector3(playerRigidbody.velocity.x, glideFallSpeed, playerRigidbody.velocity.z);
            }
        }
    }
}
