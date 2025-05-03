using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // Required if using the brightnessSlider

/// <summary>
/// Detects brightness by sampling points on a sphere around a target transform,
/// checking line of sight against specified lights. Updates calculation periodically for efficiency.
/// Includes Editor/Play Mode visualizations. Smooth intensity falloff near max range.
/// Smoothly updates an optional UI Slider.
/// </summary>
public class PlayerBrightnessDetector : MonoBehaviour
{
    // --- Constants ---
    private const float VISIBILITY_DOT_THRESHOLD = -0.1f;
    private const int MIN_SPHERE_RESOLUTION = 4;
    private const int MAX_SPHERE_RESOLUTION = 64;
    private const float GIZMO_POINT_SIZE = 0.015f;
    private const float MIN_UPDATE_INTERVAL = 0.016f; // Approx 60 FPS limit
    private const float MAX_UPDATE_INTERVAL = 1.0f;   // Max 1 sec interval

    // --- Inspector Variables ---
    [Header("Core Settings")]
    [Tooltip("Center of the detection sphere. Uses this GameObject if null.")]
    [SerializeField] private Transform detectionCenterTransform;
    [Tooltip("Lights to check against.")]
    [SerializeField] private List<Light> targetLights = new List<Light>();
    [Tooltip("Layers that block light.")]
    [SerializeField] private LayerMask obstructionLayers;
    [Tooltip("Final brightness multiplier.")]
    [Range(0f, 5f)]
    [SerializeField] private float lightIntensityMultiplier = 1f;
    [Tooltip("Normalized distance (0-1) where smooth range fade begins (e.g., 0.8 = fade starts at 80% of range). Only for Point/Spot lights.")]
    [Range(0.5f, 0.99f)]
    [SerializeField] private float rangeFadeStartFactor = 0.8f;

    [Header("Performance")]
    [Tooltip("How often (in seconds) to recalculate the brightness. Lower values are more responsive but less performant.")]
    [Range(MIN_UPDATE_INTERVAL, MAX_UPDATE_INTERVAL)]
    [SerializeField] private float updateInterval = 0.1f; // Default: 10 times per second

    [Header("Sphere Sampling Settings")]
    [Tooltip("Radius of the detection sphere.")]
    [Range(0.1f, 2.0f)]
    [SerializeField] private float detectionSphereRadius = 0.5f;
    [Tooltip("Density of sample points (Points = Res*Res). Lower for better performance.")]
    [Range(MIN_SPHERE_RESOLUTION, MAX_SPHERE_RESOLUTION)]
    [SerializeField] private int sphereResolution = 16;

    [Header("Debugging & UI")] // Renamed Header
    [Tooltip("Optional UI Slider for brightness.")]
    [SerializeField] private Slider brightnessSlider;
    [Tooltip("How quickly the UI slider interpolates to the target brightness.")]
    [Range(1f, 20f)]
    [SerializeField] private float sliderLerpSpeed = 8f; // <<< ADDED LERP SPEED
    [Tooltip("Show sphere points Gizmo in Scene view.")]
    [SerializeField] private bool visualizeSphere = true;
    [Tooltip("Show visibility lines in Play Mode (only updates with calculation interval).")]
    [SerializeField] private bool visualizeLines = false;

    // --- Private State ---
    private float _currentBrightness = 0f; // The target brightness calculated periodically
    private List<Vector3> _relativeSpherePoints = new List<Vector3>();
    private float _previousRadius;
    private int _previousResolution;
    private bool _needsPointRegeneration = true;
    private float _timeSinceLastUpdate = 0f; // Timer for periodic updates

    // --- Properties ---
    /// <summary>Gets the latest calculated brightness level (0-1).</summary>
    public float CurrentBrightness => _currentBrightness;

    // --- Unity Methods ---
    void OnValidate()
    {
        detectionSphereRadius = Mathf.Clamp(detectionSphereRadius, 0.1f, 2.0f);
        sphereResolution = Mathf.Clamp(sphereResolution, MIN_SPHERE_RESOLUTION, MAX_SPHERE_RESOLUTION);
        rangeFadeStartFactor = Mathf.Clamp(rangeFadeStartFactor, 0.5f, 0.99f);
        updateInterval = Mathf.Clamp(updateInterval, MIN_UPDATE_INTERVAL, MAX_UPDATE_INTERVAL);
        sliderLerpSpeed = Mathf.Max(1f, sliderLerpSpeed); // Ensure lerp speed is positive
        if (detectionSphereRadius != _previousRadius || sphereResolution != _previousResolution)
        {
            _needsPointRegeneration = true;
        }
    }

    void Awake()
    {
        if (detectionCenterTransform == null)
        {
            Debug.LogWarning($"PlayerBrightnessDetector on {gameObject.name}: No Detection Center Transform assigned. Using this object's transform.", this);
        }
        EnsureSpherePointsGenerated();
        _timeSinceLastUpdate = updateInterval; // Ensure first update happens immediately
    }

    void Update()
    {
        _timeSinceLastUpdate += Time.deltaTime;

        // Only perform expensive calculations periodically
        if (_timeSinceLastUpdate >= updateInterval)
        {
            _timeSinceLastUpdate -= updateInterval; // Subtract interval to maintain timing accuracy

#if UNITY_EDITOR
            EnsureSpherePointsGenerated();
#endif

            Vector3 center = GetDetectionCenter();
            if (center != Vector3.positiveInfinity)
            {
                // Calculate the new target brightness
                _currentBrightness = CalculateTotalBrightness(center);
                // NOTE: We no longer set the slider value directly here
            }
            else
            {
                // Handle invalid center case
                _currentBrightness = 0f;
            }

            // Draw debug lines only when calculation updates
            if (Application.isPlaying && visualizeLines && center != Vector3.positiveInfinity)
            {
                DrawPlayModeDebugLines(center);
            }
        }

        // --- Smoothly update the slider value every frame ---
        if (brightnessSlider != null)
        {
            // Lerp the slider's current value towards the target brightness (_currentBrightness)
            brightnessSlider.value = Mathf.Lerp(brightnessSlider.value, _currentBrightness, Time.deltaTime * sliderLerpSpeed);
        }
        // --- End Slider Update ---
    }

    // --- Core Logic ---
    private void EnsureSpherePointsGenerated()
    {
        if (_needsPointRegeneration || (_relativeSpherePoints.Count == 0 && sphereResolution > 0))
        {
            GenerateRelativeSpherePoints();
            // Update previous values after generation to sync with OnValidate check
            _previousRadius = detectionSphereRadius;
            _previousResolution = sphereResolution;
        }
    }

    private float CalculateTotalBrightness(Vector3 center)
    {
        // (Calculation logic remains the same)
        if (targetLights == null || targetLights.Count == 0) return 0f;
        if (_relativeSpherePoints.Count == 0) return 0f;

        float totalBrightness = 0f;
        foreach (Light lightSource in targetLights)
        {
            if (lightSource == null || !lightSource.enabled || !lightSource.gameObject.activeInHierarchy || lightSource.intensity <= 0) continue;
            totalBrightness += ProcessLightSource(lightSource, center);
        }
        return Mathf.Clamp01(totalBrightness * lightIntensityMultiplier);
    }

    private float ProcessLightSource(Light light, Vector3 center)
    {
        // (Calculation logic remains the same)
        Vector3 lightPos = light.transform.position;
        Vector3 dirToLight = (light.type == LightType.Directional) ? -light.transform.forward : (lightPos - center).normalized;
        float dist = (light.type == LightType.Directional) ? float.PositiveInfinity : Vector3.Distance(center, lightPos);

        float intensity = GetRawIntensityAtPoint(light, dist, dirToLight);
        if (intensity <= 0) return 0f;

        float visibility = (light.type == LightType.Directional) ? 1.0f : CalculateVisibilityFactor(light, center, dirToLight);
        return intensity * visibility;
    }

    private float GetRawIntensityAtPoint(Light light, float distance, Vector3 directionToLight)
    {
        // (Calculation logic remains the same)
        float intensity = light.intensity;
        float range = light.range;
        if ((light.type == LightType.Point || light.type == LightType.Spot) && distance >= range) return 0f;

        switch (light.type)
        {
            case LightType.Point: intensity /= Mathf.Max(1f, distance * distance); break;
            case LightType.Spot:
                float angle = Vector3.Angle(-directionToLight, light.transform.forward);
                float halfAngle = light.spotAngle * 0.5f;
                if (angle <= halfAngle)
                {
                    intensity /= Mathf.Max(1f, distance * distance);
                    float spotFalloff = Mathf.Clamp01(1.0f - (angle / halfAngle));
                    intensity *= spotFalloff * spotFalloff;
                }
                else { return 0f; }
                break;
            case LightType.Directional: break;
            default: return 0f;
        }
        if (light.type == LightType.Point || light.type == LightType.Spot)
        {
            float fadeStartDistance = range * rangeFadeStartFactor;
            if (distance > fadeStartDistance && range > fadeStartDistance)
            {
                float fadeT = Mathf.InverseLerp(fadeStartDistance, range, distance);
                intensity *= (1.0f - fadeT) * (1.0f - fadeT);
            }
        }
        return intensity;
    }


    private float CalculateVisibilityFactor(Light light, Vector3 center, Vector3 directionToLight)
    {
        // (Calculation logic remains the same)
        if (_relativeSpherePoints.Count == 0) return 0f;
        int visiblePointCount = 0;
        Vector3 lightPos = light.transform.position;
        foreach (Vector3 relPoint in _relativeSpherePoints)
        {
            Vector3 worldPoint = center + relPoint;
            if (Vector3.Dot(relPoint.normalized, directionToLight) > VISIBILITY_DOT_THRESHOLD)
            {
                bool isVisible = !Application.isPlaying || !Physics.Linecast(worldPoint, lightPos, obstructionLayers);
                if (isVisible) { visiblePointCount++; }
            }
        }
        return (_relativeSpherePoints.Count > 0) ? (float)visiblePointCount / _relativeSpherePoints.Count : 0f;
    }

    void GenerateRelativeSpherePoints()
    {
        // (Moved previousRadius/Resolution update to EnsureSpherePointsGenerated)
        if (_relativeSpherePoints == null) _relativeSpherePoints = new List<Vector3>();
        _relativeSpherePoints.Clear();
        int currentResolution = Mathf.Clamp(sphereResolution, MIN_SPHERE_RESOLUTION, MAX_SPHERE_RESOLUTION);
        int numPoints = currentResolution * currentResolution;
        if (numPoints <= 0) return;
        float phi = Mathf.PI * (3f - Mathf.Sqrt(5f));
        float currentRadius = Mathf.Max(0.01f, detectionSphereRadius);
        for (int i = 0; i < numPoints; i++)
        {
            float y = 1 - (i / (float)(numPoints - 1)) * 2;
            float radiusAtY = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y)); // Ensure sqrt >= 0
            float theta = phi * i;
            float x = Mathf.Cos(theta) * radiusAtY;
            float z = Mathf.Sin(theta) * radiusAtY;
            _relativeSpherePoints.Add(new Vector3(x, y, z) * currentRadius);
        }
        _needsPointRegeneration = false;
        // Previous values updated in EnsureSpherePointsGenerated now
    }

    private Vector3 GetDetectionCenter()
    {
        // (Remains the same)
        if (detectionCenterTransform != null) return detectionCenterTransform.position;
        if (this.transform != null) return this.transform.position;
        return Vector3.positiveInfinity;
    }

    // This method is now only called periodically from Update
    private void DrawPlayModeDebugLines(Vector3 center)
    {
        // (Debug line drawing logic remains the same)
        if (targetLights == null || _relativeSpherePoints.Count == 0) return;
        foreach (Light lightSource in targetLights)
        {
            if (lightSource == null || !lightSource.enabled || !lightSource.gameObject.activeInHierarchy) continue;
            if (lightSource.type == LightType.Point || lightSource.type == LightType.Spot)
            {
                float rangeSqr = lightSource.range * lightSource.range;
                if ((lightSource.transform.position - center).sqrMagnitude >= rangeSqr) continue;
            }
            Vector3 lightPos = lightSource.transform.position;
            Vector3 dirToLight = (lightSource.type == LightType.Directional) ? -lightSource.transform.forward : (lightPos - center);
            if (lightSource.type == LightType.Directional)
            {
                Debug.DrawLine(center, center + dirToLight.normalized * 2f, Color.yellow);
                continue;
            }
            Vector3 dirToLightNorm = dirToLight.normalized;
            foreach (Vector3 relPoint in _relativeSpherePoints)
            {
                Vector3 worldPoint = center + relPoint;
                if (Vector3.Dot(relPoint.normalized, dirToLightNorm) > VISIBILITY_DOT_THRESHOLD)
                {
                    bool isVisible = !Physics.Linecast(worldPoint, lightPos, obstructionLayers);
                    Debug.DrawLine(worldPoint, lightPos, isVisible ? Color.green : Color.red);
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        // (Gizmo code remains the same)
        if (!visualizeSphere) return;
        EnsureSpherePointsGenerated();
        if (_relativeSpherePoints == null || _relativeSpherePoints.Count == 0) return; // Check list exists before using GetDetectionCenter

        Vector3 center = GetDetectionCenter();
        if (center == Vector3.positiveInfinity) return;
        bool hasPoints = _relativeSpherePoints.Count > 0; // Simplified check
        Gizmos.color = hasPoints ? Color.cyan : Color.yellow;
        Gizmos.DrawWireSphere(center, detectionSphereRadius);
        if (hasPoints)
        {
            foreach (Vector3 relPoint in _relativeSpherePoints)
            {
                Gizmos.DrawSphere(center + relPoint, GIZMO_POINT_SIZE);
            }
        }
    }
}