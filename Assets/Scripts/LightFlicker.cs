using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light lightSource;
    public float intensityBase = 1f;
    public float intensityVariation = 0.3f;
    public float flickerSpeed = 1f;

    public float positionJitter = 0.05f;
    public float jitterSpeed = 1f;

    private Vector3 originalPosition;
    private float seed;

    void Start()
    {
        if (lightSource == null) lightSource = GetComponent<Light>();
        originalPosition = transform.localPosition;
        seed = Random.Range(0f, 1000f); // unique offset per torch
    }

    void Update()
    {
        float time = Time.time;

        // Smooth flicker
        float noise = Mathf.PerlinNoise(seed, time * flickerSpeed);
        lightSource.intensity = intensityBase + (noise - 0.5f) * 2f * intensityVariation;

        // Smooth jitter
        float x = (Mathf.PerlinNoise(seed + 1, time * jitterSpeed) - 0.5f) * 2f * positionJitter;
        float y = (Mathf.PerlinNoise(seed + 2, time * jitterSpeed) - 0.5f) * 2f * positionJitter;
        float z = (Mathf.PerlinNoise(seed + 3, time * jitterSpeed) - 0.5f) * 2f * positionJitter;

        transform.localPosition = originalPosition + new Vector3(x, y, z);
    }
}
