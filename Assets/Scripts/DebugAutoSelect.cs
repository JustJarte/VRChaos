using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DebugAutoSelect : MonoBehaviour
{
    public UnityEvent onAfterIdleForSeconds;

    private void Awake()
    {
        StartCoroutine(RaiseIdleEvent());
    }

    private IEnumerator RaiseIdleEvent()
    {
        yield return new WaitForSeconds(8.0f);

        onAfterIdleForSeconds?.Invoke();
    }
}
