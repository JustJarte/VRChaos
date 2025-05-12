using Fusion;
using UnityEngine;

// Merely used for testing a 2nd player in the Unity Editor when testing multiplayer functionality. Since I only own 1 headset, I utilize a desktop controller for the 2nd player to test movement and such.
public class DebugDesktopController : NetworkBehaviour
{
    public override void Spawned()
    {
        Debug.Log($"[Desktop] HasInputAuthority: {Object.HasInputAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"); 
        float vertical = Input.GetAxisRaw("Vertical");     

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        transform.Translate(moveDirection * 5.0f * Time.deltaTime, Space.Self);

        float rotateAmount = 0f;

        if (Input.GetMouseButton(0)) 
        {
            rotateAmount = -90.0f * Time.deltaTime;
        }
        else if (Input.GetMouseButton(1)) 
        {
            rotateAmount = 90.0f * Time.deltaTime;
        }

        if (rotateAmount != 0f)
        {
            transform.Rotate(0f, rotateAmount, 0f);
        }
    }
}
