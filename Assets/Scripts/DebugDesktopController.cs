using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDesktopController : NetworkBehaviour
{
    public override void Spawned()
    {
        Debug.Log($"[Desktop] HasInputAuthority: {Object.HasInputAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        // Move in local space
        transform.Translate(moveDirection * 5.0f * Time.deltaTime, Space.Self);

        float rotateAmount = 0f;

        if (Input.GetMouseButton(0)) // Left click
        {
            rotateAmount = -90.0f * Time.deltaTime;
        }
        else if (Input.GetMouseButton(1)) // Right click
        {
            rotateAmount = 90.0f * Time.deltaTime;
        }

        if (rotateAmount != 0f)
        {
            transform.Rotate(0f, rotateAmount, 0f);
        }
    }
}
