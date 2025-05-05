using Fusion;
using GorillaLocomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotController : NetworkBehaviour
{
    [SerializeField] private Player player;

    [Networked] private TickTimer moveTimer { get; set; }

    public void InitializeAsBot()
    {
        moveTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && moveTimer.Expired(Runner))
        {
            Vector3 randomOffset = new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));

            transform.position += randomOffset.normalized * 1.0f;

            moveTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);
        }
    }
}
