using UnityEngine;
using System.Collections;
using System;
using FMODUnity;
using SteamAudio;
using Vector3 = UnityEngine.Vector3;
/// <summary>
/// Handles the sounds played in the game.
/// </summary>
public class SoundGameplayManager : MonoBehaviour
{
    [Header("Gameplay Settings")]
    // Adjust the sound volume based on the distance from the sound source.
    [SerializeField, Tooltip("0 = close, 1 = far")] 
    private AnimationCurve distanceCurve;

    [Space(10f)]
    [Header("Guard Settings")]
    [field: SerializeField] private EventReference guardFootsteps;

    [Space(10f)]
    [Header("Player Settings")]
    [field: SerializeField] private EventReference playerFootsteps;
    [SerializeField] private float rangeMultiplier = 1f;
    [SerializeField] private float amplitudeMultiplier = 1f;
    // Based on the magnitude of the step, we want to customize the sound.
    [Header("Step Settings")]
    [SerializeField] private float minMagnitude = 0.1f; // Minimum magnitude to play a sound
    [SerializeField] private float maxMagnitude = 1f; // the maximum player step magnitude
    // Curves used to scale the sounds based on the magnitude of the step.
    [SerializeField, Tooltip("0 = min, 1 = max")] 
    private AnimationCurve rangeCurve;
    [SerializeField, Tooltip("0 = min, 1 = max")] 
    private AnimationCurve amplitudeCurve;

    [Space(10f)]
    [Header("Material Settings")]
    [SerializeField] private MaterialSettings carpetSettings;
    [SerializeField] private MaterialSettings metalSettings;
    [SerializeField] private MaterialSettings stoneSettings;
    [SerializeField] private MaterialSettings waterSettings;
    [SerializeField] private MaterialSettings woodSettings;

    // Constants
    private struct PARAM
    { 
        public const string SURFACE = "SurfaceType";
        public const string MAGNITUDE = "Magnitude";
    }

    // Singelton instance
    public static SoundGameplayManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Private:

    [Serializable]
    private struct MaterialSettings
    {
        public float Range;
        public float Amplitude;
    }

    /// <summary>
    /// Returns the index used to play the correct material sound for FMOD.
    /// </summary>
    private static int GetMaterialIndex(EMaterialTag material)
    {
        switch (material)
        {
            case EMaterialTag.Carpet:
                return 0;
            case EMaterialTag.Metal:
                return 1;
            case EMaterialTag.Stone:
                return 2;
            case EMaterialTag.Water:
                return 3;
            case EMaterialTag.Wood:
                return 4;
        }

        Debug.LogError("Invalid material tag");
        return -1;
    }

    /// <summary>
    /// Based on the material tag, returns the correct material settings.
    /// It also scales the settings based on the magnitude of the step.
    /// Configurable in the inspector.
    /// </summary>
    private MaterialSettings GetPlayerMaterialSettingsScaled(EMaterialTag mat, float mag)
    {
        MaterialSettings settings = mat switch
        {
            EMaterialTag.Carpet => carpetSettings,
            EMaterialTag.Metal => metalSettings,
            EMaterialTag.Stone => stoneSettings,
            EMaterialTag.Water => waterSettings,
            EMaterialTag.Wood => woodSettings,
            // Default case to Wood
            _ => woodSettings
        };
        // Scale the settings based on the magnitude of the step.
        settings.Range *= rangeCurve.Evaluate(mag) * rangeMultiplier;
        settings.Amplitude *= amplitudeCurve.Evaluate(mag) * amplitudeMultiplier;
        return settings;
    }

    /// <summary>
    /// FMOD magnitude only goes from 0 to 1, so we need to scale the magnitude.
    /// It should also be scaled by the min and max magnitude settings.
    /// Those settings define the expected range of the step velocity magnitude.
    /// </summary>
    private float GetMagnitudeScaled(float mag)
    {
        float scaledMag = (mag - minMagnitude) / (maxMagnitude - minMagnitude);
        return Mathf.Clamp01(scaledMag);
    }

    // Public:

    /// <summary>
    /// Used to get the right material settings for the sound.
    /// </summary>
    public EMaterialTag TryGetMaterialFromTag(string tag)
    {
        switch (tag)
        {
            case "Wood":
                return EMaterialTag.Wood;
            case "Metal":
                return EMaterialTag.Metal;
            case "Stone":
                return EMaterialTag.Stone;
            case "Water":
                return EMaterialTag.Water;
            case "Carpet":
                return EMaterialTag.Carpet;
        }
        return EMaterialTag.Wood; // Default to Wood if invalid
    }

    public enum EMaterialTag
    {
        None = -1,
        Wood,
        Metal,
        Stone,
        Water,
        Carpet
    }

    /// <summary>
    /// Plays the player step sound at the given position, with the correct material settings.
    /// </summary>
    public void PlayPlayerStepAtPosition(StudioEventEmitter emitter, EMaterialTag mat, Vector3 pos, float mag)
    {
        // First, create and play the game logic sound.
        var settings = GetPlayerMaterialSettingsScaled(mat, GetMagnitudeScaled(mag));
        var sound = new Sound(pos, settings.Range, settings.Amplitude);
        sound.SoundType = Sound.ESoundType.Player;
        Sounds.MakeSound(sound);

        // Then, play the FMOD sound.
        emitter.EventReference = playerFootsteps;
        // FMOD event emitter clears the parameters when played,
        // so we need to set them after we play the sound.
        emitter.Play();
        emitter.SetParameter(PARAM.SURFACE, GetMaterialIndex(mat));
        emitter.SetParameter(PARAM.MAGNITUDE, GetMagnitudeScaled(mag));
    }

    public void PlayGuardStep(StudioEventEmitter emitter, EMaterialTag mat)
    {
        // We dont need game logic sound rn
        // Play the FMOD sound.
        emitter.EventReference = guardFootsteps;
        emitter.Play();
        emitter.SetParameter(PARAM.SURFACE, GetMaterialIndex(mat));
        emitter.SetParameter(PARAM.MAGNITUDE, 0.5f);
    }

    /// <summary>
    /// Gets the volume of the sound at the given position, 
    /// scaled by gameplay settings.
    /// </summary>
    public float GetSoundVolume(Sound sound, Vector3 position)
    {
        // Find the amount the detection value should be increased
        var lerpValue = Vector3.Distance(sound.Pos, position) / sound.Range;
        return sound.Amplitude * distanceCurve.Evaluate(lerpValue);
    }
}