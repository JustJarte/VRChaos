using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// The Pull Piece of the Tranq Crossbow weapon. Based on whether this part is Primed or not determines whether the holding Player can fire a Tranq Dart. Has a slight cooldown between shots which is made evident by the Pull Piece being pulled back into
// place to load another dart. 
public class TranqDartGunPullPiece : MonoBehaviour
{
    [SerializeField] private Transform primedPosition;
    [SerializeField] private GameObject dartMeshPreview;

    [Space(10.0f)] public UnityEvent onIsFullyPrimed;

    private Vector3 initialLocalPosition;
    private float pullDuration = 2.0f;

    public bool IsPrimed { get; set; }

    // Get starting position and set anchor to default state.
    private void Start()
    {
        initialLocalPosition = transform.localPosition;

        ReleaseAnchor();
    }

    // Releases the Pull Piece anchor as if the weapon was fired. If it was fully Primed, additionally we have a UnityEvent that can be Invoked to do other stuff if desired. Once its been fired, it flags itself as not Primed
    // hides the dart mesh that shows when another has been loaded, and starts a Coroutine to start pulling the anchor back again over the defined pullDuration.
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

    // Essentially animates the anchor being pulled back to prepare another dart and act as a cooldown so that the weapon cannot be fired continuously. Once it's fully pulled back, we flag it as Primed, and then set the
    // tranq dart mesh to show the Player can fire the weapon again.
    private IEnumerator StartPullingAnchorBack()
    {
        yield return new WaitForSeconds(0.25f);

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

        IsPrimed = true;
        dartMeshPreview.SetActive(true);
    }
}
