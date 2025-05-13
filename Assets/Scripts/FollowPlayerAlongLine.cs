using UnityEngine;

/// <summary>
/// Follows the player along a line defined by LinePoints.
/// Includes debugging visualizations.
/// </summary>
public class FollowPlayerAlongLine : MonoBehaviour
{
    [Header("Object to Move")]
    [SerializeField] private Transform FollowObject;

    [Header("Path Definition")]
    [SerializeField] private Transform[] LinePoints;

    [Header("Movement Settings")]
    [SerializeField] private float FollowSpeed = 5f;

    [Header("Debugging")]
    [Tooltip("Enable to draw debug lines and spheres in the editor.")]
    public bool EnableDebugging = true;
    [SerializeField] private Color PathColor = Color.green;
    [SerializeField] private Color TargetLineColor = Color.yellow;
    [SerializeField] private Color TargetPointColor = Color.red;
    [SerializeField] private float TargetPointRadius = 0.25f;

    private Transform playerTransform;

    // Cached data for performance
    private Vector3[] lineWorldPositions; // Stores the world positions of the line points
    private bool isPathValid = false;     // Flag to indicate if the path is usable

    private void Start()
    {
        // --- Initialization and Validation ---
        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.Transform;
        }
        else
        {
            Debug.LogError("[FollowPlayerAlongLine] PlayerController.Instance is null. Cannot find player. Disabling script.", this);
            enabled = false; // Disable this script component
            return;
        }

        if (FollowObject == null)
        {
            Debug.LogError("[FollowPlayerAlongLine] FollowObject is not assigned. Disabling script.", this);
            enabled = false;
            return;
        }

        InitializePath();

        if (EnableDebugging)
        {
            Debug.Log($"[FollowPlayerAlongLine] Debugging enabled. Path valid: {isPathValid}. Path points: {(lineWorldPositions != null ? lineWorldPositions.Length : 0)}", this);
        }
    }

    /// <summary>
    /// Validates the LinePoints array and caches their world positions.
    /// </summary>
    void InitializePath()
    {
        if (LinePoints == null || LinePoints.Length == 0)
        {
            Debug.LogWarning("[FollowPlayerAlongLine] LinePoints array is null or empty. Follow behavior will be disabled.", this);
            isPathValid = false;
            return;
        }

        lineWorldPositions = new Vector3[LinePoints.Length];
        for (int i = 0; i < LinePoints.Length; i++)
        {
            if (LinePoints[i] == null)
            {
                Debug.LogError($"[FollowPlayerAlongLine] LinePoints element at index {i} is null. Path initialization failed. Disabling follow behavior.", this);
                isPathValid = false;
                return;
            }
            lineWorldPositions[i] = LinePoints[i].position;
        }

        if (LinePoints.Length == 1)
        {
            Debug.LogWarning("[FollowPlayerAlongLine] LinePoints array only has one point. FollowObject will target this single point.", this);
            // A single point path is technically valid for targeting.
        }
        else if (LinePoints.Length < 2) // Should be caught by Length == 0 already, but as a safeguard.
        {
            Debug.LogWarning($"[FollowPlayerAlongLine] LinePoints array has {LinePoints.Length} points. At least 2 are needed for a line segment. Follow behavior might be limited or disabled.", this);
            // isPathValid might be set to false or handled based on desired behavior for <2 points.
            // For now, we proceed if Length is 1, as handled above. If 0, it's already returned.
        }

        isPathValid = true;
        if (EnableDebugging) Debug.Log($"[FollowPlayerAlongLine] Path initialized with {lineWorldPositions.Length} points.", this);
    }

    private void Update()
    {
        if (!isPathValid || playerTransform == null || FollowObject == null)
        {
            return;
        }

        Vector3 targetPositionOnPath;

        if (lineWorldPositions.Length == 1)
        {
            targetPositionOnPath = lineWorldPositions[0];
        }
        else if (lineWorldPositions.Length > 1)
        {
            targetPositionOnPath = GetClosestPointOnPolyline(playerTransform.position, lineWorldPositions);
        }
        else
        {
            // This case should ideally not be reached if isPathValid is true
            // and InitializePath has correctly set up or invalidated the path.
            if (EnableDebugging) Debug.LogWarning("[FollowPlayerAlongLine] Update: Path has insufficient points, though isPathValid was true. This indicates an issue.", this);
            return;
        }

        FollowObject.position = Vector3.Lerp(FollowObject.position, targetPositionOnPath, Time.deltaTime * FollowSpeed);

        if (EnableDebugging)
        {
            DrawDebugVisualizations(targetPositionOnPath);
        }
    }

    /// <summary>
    /// Draws debug lines and spheres in the Scene view if debugging is enabled.
    /// </summary>
    private void DrawDebugVisualizations(Vector3 targetPositionOnPath)
    {
        // Draw the path segments
        if (lineWorldPositions != null && lineWorldPositions.Length > 1)
        {
            for (int i = 0; i < lineWorldPositions.Length - 1; i++)
            {
                Debug.DrawLine(lineWorldPositions[i], lineWorldPositions[i + 1], PathColor);
            }
        }
        else if (lineWorldPositions != null && lineWorldPositions.Length == 1)
        {
            // Optionally draw the single point if desired
            Debug.DrawRay(lineWorldPositions[0] - Vector3.up * 0.5f, Vector3.up, PathColor, 0f, false);
        }


        // Draw a line from player to the target point on the path
        if (playerTransform != null)
        {
            Debug.DrawLine(playerTransform.position, targetPositionOnPath, TargetLineColor);
        }

        // Draw a sphere at the target point on the path
        Debug.DrawRay(targetPositionOnPath - Vector3.up * TargetPointRadius, Vector3.up * TargetPointRadius * 2f, TargetPointColor);
        Debug.DrawRay(targetPositionOnPath - Vector3.right * TargetPointRadius, Vector3.right * TargetPointRadius * 2f, TargetPointColor);
        Debug.DrawRay(targetPositionOnPath - Vector3.forward * TargetPointRadius, Vector3.forward * TargetPointRadius * 2f, TargetPointColor);

        // For a sphere (visible only if Gizmos are enabled and editor is playing, or use OnDrawGizmos)
        // Gizmos.color = TargetPointColor;
        // Gizmos.DrawSphere(targetPositionOnPath, TargetPointRadius);
        // Note: Gizmos.DrawSphere is better drawn in OnDrawGizmos for persistent visualization.
        // Debug.DrawRay is used here for simplicity within Update if Gizmos are not always visible.
    }


    private Vector3 GetClosestPointOnPolyline(Vector3 worldPoint, Vector3[] polylineWorldPoints)
    {
        Vector3 closestPointOverall = Vector3.zero;
        float minDistanceSqr = float.MaxValue;

        for (int i = 0; i < polylineWorldPoints.Length - 1; i++)
        {
            Vector3 segmentStart = polylineWorldPoints[i];
            Vector3 segmentEnd = polylineWorldPoints[i + 1];

            Vector3 closestPointOnCurrentSegment = GetClosestPointOnLineSegment(worldPoint, segmentStart, segmentEnd);
            float distSqrToSegment = (worldPoint - closestPointOnCurrentSegment).sqrMagnitude;

            if (distSqrToSegment < minDistanceSqr)
            {
                minDistanceSqr = distSqrToSegment;
                closestPointOverall = closestPointOnCurrentSegment;
            }
        }
        // if (EnableDebugging)
        // {
        //    Debug.Log($"[FollowPlayerAlongLine] Closest point on polyline found at {closestPointOverall} for player at {worldPoint}", this);
        // }
        return closestPointOverall;
    }

    private Vector3 GetClosestPointOnLineSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 segmentDirection = b - a;
        Vector3 pointRelativeStart = p - a;

        float segmentSqrMagnitude = segmentDirection.sqrMagnitude;

        if (segmentSqrMagnitude < 0.000001f)
            return a;

        float t = Vector3.Dot(pointRelativeStart, segmentDirection) / segmentSqrMagnitude;

        if (t < 0.0f)
        {
            return a;
        }
        else if (t > 1.0f)
        {
            return b;
        }
        else
        {
            return a + t * segmentDirection;
        }
    }

    // Optional: If you want gizmos that are always visible even when the game object is not selected
    // and also when the game is not playing (for path setup).
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!EnableDebugging || LinePoints == null || LinePoints.Length == 0)
        {
            // Attempt to draw current path if LinePoints are set, even if not playing
            // This helps in setting up the path in the editor.
            if (LinePoints != null && LinePoints.Length > 0)
            {
                Gizmos.color = PathColor;
                Vector3 previousPoint = Vector3.zero; // Placeholder for the first point
                bool firstPointSet = false;

                for (int i = 0; i < LinePoints.Length; i++)
                {
                    if (LinePoints[i] == null) continue; // Skip null points
                    
                    Vector3 currentPoint = LinePoints[i].position;
                    Gizmos.DrawSphere(currentPoint, TargetPointRadius * 0.5f); // Draw a small sphere at each path node

                    if (firstPointSet && i > 0)
                    {
                        Gizmos.DrawLine(previousPoint, currentPoint);
                    }
                    previousPoint = currentPoint;
                    firstPointSet = true;
                }
            }
            return;
        }

        // Draw runtime path if lineWorldPositions is initialized (i.e., game is playing or has played)
        if (isPathValid && lineWorldPositions != null && lineWorldPositions.Length > 1)
        {
            Gizmos.color = PathColor;
            for (int i = 0; i < lineWorldPositions.Length - 1; i++)
            {
                Gizmos.DrawLine(lineWorldPositions[i], lineWorldPositions[i + 1]);
            }
        }
        else if (isPathValid && lineWorldPositions != null && lineWorldPositions.Length == 1)
        {
            Gizmos.color = PathColor;
            Gizmos.DrawSphere(lineWorldPositions[0], TargetPointRadius);
        }

        // To draw the target point and line from player in Gizmos, you'd need access to 
        // playerTransform.position and the calculated targetPositionOnPath.
        // This is easier with Debug.DrawLine in Update for runtime dynamic elements.
        // However, you could cache the last known targetPositionOnPath if needed here.
    }
#endif
}