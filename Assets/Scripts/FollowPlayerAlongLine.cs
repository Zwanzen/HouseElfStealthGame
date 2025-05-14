using UnityEngine;

/// <summary>
/// Follows the player along a defined path of LinePoints.
/// Optimized for performance and simplified for clarity.
/// </summary>
public class FollowPlayerAlongLine : MonoBehaviour
{
    [Header("Object to Move")]
    [SerializeField] private Transform FollowObject;

    [Header("Path Definition")]
    [SerializeField] private Transform[] LinePoints; // At least 2 points for a line, 1 for a single target point.

    [Header("Movement Settings")]
    [SerializeField] private float FollowSpeed = 5f;

    private Transform playerTransform;
    private Vector3[] lineWorldPositions; // Cached world positions of the line points
    private bool isPathValid = false;

    private void Start()
    {
        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.Transform;
        }
        else
        {
            Debug.LogError("[FollowPlayerAlongLine] PlayerController.Instance is null. Script disabled.", this);
            enabled = false;
            return;
        }

        if (FollowObject == null)
        {
            Debug.LogError("[FollowPlayerAlongLine] FollowObject is not assigned. Script disabled.", this);
            enabled = false;
            return;
        }

        InitializePath();
    }

    void InitializePath()
    {
        if (LinePoints == null || LinePoints.Length == 0)
        {
            Debug.LogWarning("[FollowPlayerAlongLine] LinePoints array is null or empty. Path is invalid.", this);
            isPathValid = false;
            return;
        }

        lineWorldPositions = new Vector3[LinePoints.Length];
        for (int i = 0; i < LinePoints.Length; i++)
        {
            if (LinePoints[i] == null)
            {
                Debug.LogError($"[FollowPlayerAlongLine] LinePoints element at index {i} is null. Path is invalid.", this);
                isPathValid = false;
                return;
            }
            lineWorldPositions[i] = LinePoints[i].position;
        }

        isPathValid = true;
    }

    private void Update()
    {
        if (!isPathValid || playerTransform == null) // FollowObject null check is in Start
        {
            return;
        }

        Vector3 targetPositionOnPath;

        if (lineWorldPositions.Length == 1)
        {
            targetPositionOnPath = lineWorldPositions[0];
        }
        else // Requires at least 2 points for GetClosestPointOnPolyline
        {
            targetPositionOnPath = GetClosestPointOnPolyline(playerTransform.position, lineWorldPositions);
        }

        FollowObject.position = Vector3.Lerp(FollowObject.position, targetPositionOnPath, Time.deltaTime * FollowSpeed);
    }

    private Vector3 GetClosestPointOnPolyline(Vector3 worldPoint, Vector3[] polylinePoints)
    {
        Vector3 closestPointOverall = polylinePoints[0]; // Initialize with the first point
        float minDistanceSqr = (worldPoint - closestPointOverall).sqrMagnitude;

        for (int i = 0; i < polylinePoints.Length - 1; i++)
        {
            Vector3 segmentStart = polylinePoints[i];
            Vector3 segmentEnd = polylinePoints[i + 1];

            Vector3 closestPointOnSegment = GetClosestPointOnLineSegment(worldPoint, segmentStart, segmentEnd);
            float distSqrToSegment = (worldPoint - closestPointOnSegment).sqrMagnitude;

            if (distSqrToSegment < minDistanceSqr)
            {
                minDistanceSqr = distSqrToSegment;
                closestPointOverall = closestPointOnSegment;
            }
        }
        return closestPointOverall;
    }

    private Vector3 GetClosestPointOnLineSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 segmentDirection = b - a;
        Vector3 pointRelativeStart = p - a;

        float segmentSqrMagnitude = segmentDirection.sqrMagnitude;

        if (segmentSqrMagnitude < 0.000001f) // Segment is a point
            return a;

        float t = Vector3.Dot(pointRelativeStart, segmentDirection) / segmentSqrMagnitude;

        if (t < 0.0f) return a;
        if (t > 1.0f) return b;
        return a + t * segmentDirection;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (LinePoints == null || LinePoints.Length == 0) return;

        Gizmos.color = Color.cyan; // Path color for editor visualization
        Vector3 previousPoint = Vector3.zero;
        bool firstPointValid = false;

        for (int i = 0; i < LinePoints.Length; i++)
        {
            if (LinePoints[i] == null) continue;
            
            Vector3 currentPoint = LinePoints[i].position;
            Gizmos.DrawSphere(currentPoint, 0.1f);

            if (firstPointValid && i > 0)
            {
                 Gizmos.DrawLine(previousPoint, currentPoint);
            }
            previousPoint = currentPoint;
            firstPointValid = true;
        }

        // If playing and path is valid, you could also draw the cached lineWorldPositions
        if (Application.isPlaying && isPathValid && lineWorldPositions != null && lineWorldPositions.Length > 1)
        {
            Gizmos.color = Color.green; // Runtime path color
             for (int i = 0; i < lineWorldPositions.Length - 1; i++)
            {
                Gizmos.DrawLine(lineWorldPositions[i], lineWorldPositions[i + 1]);
            }
        }
    }
#endif
}