using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Merely used for testing in Unity Editor. Put on a GameObject in the Lobby scene to automatically have it choose a stage for me without any input so I don't have to swap between 2 game instances every time
// I want to test something.
public class DebugAutoSelect : MonoBehaviour
{
    [SerializeField] private float idleForSeconds = 8.0f;

    public UnityEvent onAfterIdleForSeconds;

    private void Awake()
    {
        StartCoroutine(RaiseIdleEvent());
    }

    private IEnumerator RaiseIdleEvent()
    {
        yield return new WaitForSeconds(idleForSeconds);

        onAfterIdleForSeconds?.Invoke();
    }
}
