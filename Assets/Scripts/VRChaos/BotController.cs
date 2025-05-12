using Fusion;
using GorillaLocomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls a bot instance of a Player character to test mechanics and functionalities.
public class BotController : NetworkBehaviour
{
    [SerializeField] private float delayBetweenMovement = 1.5f;

    [Networked] private TickTimer MoveTimer { get; set; }

    // Initializes the bot when spawned from NetworkRunnerHandler and creates the initial TickTimer.
    public void InitializeAsBot()
    {
        MoveTimer = TickTimer.CreateFromSeconds(Runner, delayBetweenMovement);
    }

    // Every time the TickTimer has expired, move the bot randomly a little bit on the x and z axis, then start the TickTimer again with the same amount of time between movement.
    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && MoveTimer.Expired(Runner))
        {
            Vector3 randomOffset = new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));

            transform.position += randomOffset.normalized * 1.0f;

            MoveTimer = TickTimer.CreateFromSeconds(Runner, delayBetweenMovement);
        }
    }
}
