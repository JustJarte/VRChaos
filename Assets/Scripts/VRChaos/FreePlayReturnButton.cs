using Fusion;
using GorillaLocomotion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Script solely for Free Play Mode currently. In Free Play Mode, since there's no match or conditions to end the game, Players are allowed to tap an "escape" button located in the center of the map near the picnic table to ping the Host and allow them to
// leave the Free Play Mode session and return to their lobby. 
public class FreePlayReturnButton : NetworkBehaviour
{
    [SerializeField] private Transform unpressedTransform;
    [SerializeField] private Transform pressedTransform;

    private bool isAnimating = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isAnimating) return;

        Player player = other.GetComponentInParent<Player>();

        if (player != null && player.Object.HasInputAuthority)
        {
            StartCoroutine(AnimatePressAndReturn(player.Object.InputAuthority));
        }
    }

    // Animates the button being pressed down when "tapped" in OnTriggerEnter and then returns that player to their lobby.
    private IEnumerator AnimatePressAndReturn(PlayerRef player)
    {
        isAnimating = true;

        float t = 0.0f;

        while (t < 1.0f)
        {
            t += Time.deltaTime * 1.0f;

            transform.localPosition = Vector3.Lerp(unpressedTransform.localPosition, pressedTransform.localPosition, t);

            yield return null;
        }

        yield return new WaitForSeconds(1.0f);

        t = 0.0f;

        while (t < 1.0f)
        {
            t += Time.deltaTime * 1.0f;

            transform.localPosition = Vector3.Lerp(pressedTransform.localPosition, unpressedTransform.localPosition, t);

            yield return null;
        }

        isAnimating = false;

        FreePlayModeManager.Instance?.ReturnToLobbyRequest(player);
    }
}
