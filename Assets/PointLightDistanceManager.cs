using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Required for .Where()

/// <summary>
/// Manages Point Lights in the scene, turning them on or off based on their
/// distance to a target transform (defaults to this GameObject's transform).
/// Checks are performed at a specified interval.
/// </summary>
public class PointLightDistanceManager : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The transform to measure distance from. If null, uses this GameObject's transform.")]
    public Transform targetTransform;

    [Header("Light Control Settings")]
    [Tooltip("The maximum distance at which lights will be turned on.")]
    public float activationDistance = 20f;

    [Tooltip("How often (in seconds) to check the distance and toggle lights.")]
    public float checkInterval = 1.0f;

    // Private list to store all relevant point lights in the scene
    private List<Light> _pointLights = new List<Light>();
    // Cache the WaitFoSeconds object to avoid repeated allocations
    private WaitForSeconds _waitForInterval;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        // If no target transform is assigned, use the transform of this GameObject
        if (targetTransform == null)
        {
            targetTransform = transform;
            Debug.Log("Target Transform not set. Defaulting to this GameObject: " + gameObject.name);
        }

        _waitForInterval = new WaitForSeconds(checkInterval);
    }

    /// <summary>
    /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// </summary>
    void Start()
    {
        FindAllPointLights();

        if (_pointLights.Count == 0)
        {
            Debug.LogWarning("No Point Lights found in the scene. The script will not manage any lights.");
            // Optionally, disable the script if no lights are found to save resources
            // enabled = false;
            // return;
        }
        else
        {
            Debug.Log($"Found {_pointLights.Count} Point Lights to manage.");
        }

        // Start the coroutine that periodically checks light distances
        StartCoroutine(CheckLightDistancesRoutine());
    }

    /// <summary>
    /// Finds all Light components in the scene and filters for Point Lights.
    /// </summary>
    void FindAllPointLights()
    {
        // Find all objects with a Light component
        Light[] allLightsInScene = FindObjectsOfType<Light>();

        // Filter for only Point Lights and add them to our list
        _pointLights = allLightsInScene.Where(light => light.type == LightType.Point).ToList();

        // Initial check to set lights based on distance at startup
        UpdateLightsState();
    }

    /// <summary>
    /// Coroutine that periodically calls UpdateLightsState.
    /// </summary>
    /// <returns>IEnumerator for the coroutine.</returns>
    IEnumerator CheckLightDistancesRoutine()
    {
        // Infinite loop that runs as long as the script is active
        while (true)
        {
            UpdateLightsState();
            // Wait for the specified interval before checking again
            yield return _waitForInterval;
        }
    }

    /// <summary>
    /// Checks the distance to each point light and enables/disables them accordingly.
    /// </summary>
    void UpdateLightsState()
    {
        if (targetTransform == null)
        {
            Debug.LogError("Target Transform is null. Cannot update light states.");
            return;
        }

        Vector3 targetPosition = targetTransform.position;

        foreach (Light light in _pointLights)
        {
            if (light == null) continue; // Skip if a light was destroyed

            // Calculate the distance from the target to the light
            float distanceToLight = Vector3.Distance(targetPosition, light.transform.position);

            // Check if the light should be on or off
            if (distanceToLight <= activationDistance)
            {
                // If the light is within activation distance and not already enabled, turn it on
                if (!light.enabled)
                {
                    light.enabled = true;
                }
            }
            else
            {
                // If the light is outside activation distance and not already disabled, turn it off
                if (light.enabled)
                {
                    light.enabled = false;
                }
            }
        }
    }

    /// <summary>
    /// Public method to manually refresh the list of point lights.
    /// Useful if new point lights are instantiated at runtime.
    /// </summary>
    public void RefreshPointLights()
    {
        Debug.Log("Refreshing point lights list...");
        _pointLights.Clear();
        FindAllPointLights();
        Debug.Log($"Found {_pointLights.Count} Point Lights after refresh.");
    }


    // Optional: Draw a gizmo in the editor to visualize the activation distance
    void OnDrawGizmosSelected()
    {
        if (targetTransform != null)
        {
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.3f); // Yellow, semi-transparent
            Gizmos.DrawWireSphere(targetTransform.position, activationDistance);
        }
    }
}
