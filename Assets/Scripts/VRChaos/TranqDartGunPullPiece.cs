using Fusion.XR.Shared.Rig;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TranqDartGunPullPiece : MonoBehaviour
{
    [SerializeField] private Transform primedPosition;
    [SerializeField] private GameObject dartMeshPreview;

    [Space(10.0f)] public UnityEvent onIsFullyPrimed;

    private bool IsPulling { get; set; } = false;
    private Vector3 initialLocalPosition;
    private float pullDuration = 2.0f;

    public bool IsPrimed { get; set; }

    private void Start()
    {
        initialLocalPosition = transform.localPosition;

        ReleaseAnchor();
    }

    public void ReleaseAnchor()
    {
        if (IsPrimed)
        {
            onIsFullyPrimed?.Invoke();
        }

        transform.localPosition = initialLocalPosition;
        IsPrimed = false;

        dartMeshPreview.SetActive(false);

        StartCoroutine(StartPullingAnchorBack());
    }

    private IEnumerator StartPullingAnchorBack()
    {
        yield return new WaitForSeconds(0.25f);

        IsPulling = true;

        float timer = 0.0f;

        Vector3 startPos = transform.localPosition;
        Vector3 endPos = primedPosition.localPosition;

        while (timer < pullDuration)
        {
            timer += Time.deltaTime;
            float t = timer / pullDuration;

            transform.localPosition = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        transform.localPosition = endPos;
        IsPulling = false;
        IsPrimed = true;
        dartMeshPreview.SetActive(true);
    }
}
