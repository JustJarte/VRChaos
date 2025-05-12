using UnityEngine;

// Simply animates a draw string for the crossbow weapon. Purely aesthetic, it merely followed the center point, which is the Pull Piece, which moves along a track, but can become more defined with more positions and such as preferred.
public class TranqDartGunBowstring : MonoBehaviour
{
    [SerializeField] private Transform leftBowgunAnchor;
    [SerializeField] private Transform rightBowgunAnchor;
    [SerializeField] private Transform centerPoint;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int curveResolution = 7;
    [SerializeField] private float maxCurveDepth = 0.05f;

    private Vector3[] positions;

    private void Awake()
    {
        lineRenderer.positionCount = curveResolution;
        lineRenderer.useWorldSpace = false;
        positions = new Vector3[curveResolution];
    }

    // Keeps track of the anchors, and animates the string being pulled back as the Pull Piece moves, and then released.
    private void LateUpdate()
    {
        Vector3 start = transform.InverseTransformPoint(leftBowgunAnchor.position);
        Vector3 end = transform.InverseTransformPoint(rightBowgunAnchor.position);
        Vector3 center = transform.InverseTransformPoint(centerPoint.position);

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector3 straightLine = Vector3.Lerp(start, end, t);

            Vector3 mid = (start + end) / 2.0f;
            float pullAmount = Vector3.Distance(center, mid);

            Vector3 curveOffset = Vector3.back * Mathf.Sin(t * Mathf.PI) * pullAmount * maxCurveDepth;

            positions[i] = straightLine + curveOffset;
        }

        lineRenderer.SetPositions(positions);
    }
}
