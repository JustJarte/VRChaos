using GorillaLocomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
