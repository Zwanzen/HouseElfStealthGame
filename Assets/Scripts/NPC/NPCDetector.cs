using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class NPCDetector
{
    private NPC _npc;
    private SoundGameplayManager soundManager;
    private PlayerController _player;
    private PlayerBrightnessDetector _brightnessDetector;
    private Slider _slider;
    private Transform[] _limbs;
    private LayerMask _obstacleLayerMask;

    public NPCDetector(NPC npc, PlayerController player, Slider slider, LayerMask Obsticle)
    {
        _npc = npc;
        _player = player;
        _brightnessDetector = player.GetComponent<PlayerBrightnessDetector>();
        soundManager = SoundGameplayManager.Instance;
        _slider = slider;
        _limbs = player.Limbs;
        _obstacleLayerMask = Obsticle;
    }

    public enum EDetectionState
    {
        Default, // Does his job, patrols, etc.
        Curious, // Will stop and look around
        Alert, // Will look for the player
    }

    // ___ Private Variables ___
    private float _detectionDecaySpeed = 10f; // The amount of detection to decay per second
    // The amount of time to wait before decaying the detection
    private float _detectionDecayDelay
    {
        get
        {
            if (DetectionState == EDetectionState.Alert)
                return 5f;
            else if (DetectionState == EDetectionState.Curious)
                return 5f;
            else
                return 1f;
        }
    } 
    private float _decayTimer = 0f;
    private float _bufferTime = 0.2f; // The amount of time to wait before detecting the sound

    // ___ Events ___
    public event Action<EDetectionState> OnDetectionStateChanged;

    // ___ Properties ___
    public float MaxPlayerAngle => _npc.Type == NPC.NPCType.Sleep ? 85f : 45f;
    /// <summary>
    /// The current detection level of the NPC.
    /// </summary>
    public float Detection { get; private set; }
    /// <summary>
    /// The current detection state of the NPC. Changes behavior.
    /// </summary>
    public EDetectionState DetectionState { get; private set; } = EDetectionState.Default;
    /// <summary>
    /// The point of interest for the NPC. This is the last known position of sound or sight.
    /// </summary>
    public Vector3 POI { get; private set; }
    public bool PlayerInSight => IsPlayerVisible();


    // ___ Private Methods ___
    /// <summary>
    /// Main method to add detection to the NPC.
    /// </summary>
    private void AddDetection(float amount, Vector3 pos)
    {
        Detection = Mathf.Clamp(Detection + amount, 0f, 1f);
        // Set decay timer to 0
        _decayTimer = 0f;
        // Update to correct state, setting to default handled elsewhere
        if (Detection > 0.66f)
            UpdateDetection(EDetectionState.Alert, pos);
        else if (Detection > 0.33f)
            UpdateDetection(EDetectionState.Curious, pos);

    }

    private void UpdateDetection(EDetectionState state, Vector3 pos)
    {
        DetectionState = state;
        POI = pos;
        OnDetectionStateChanged?.Invoke(DetectionState);
    }

    /// <summary>
    /// Handle the sound decay of the NPC.
    /// </summary>
    private void HandleDetectionDecay(float delta)
    {
        // If we are in alert state, we dont want to decay the detection
        // Movement will tell state to be curious, then we will decay
        // Unless we are sleeping, then we want to decay
        if (DetectionState == EDetectionState.Alert && _npc.Type != NPC.NPCType.Sleep)
            return;

        // If the decay timer is greater than the decay delay, decay the detection
        if (_decayTimer >= _detectionDecayDelay)
            Detection = Mathf.Clamp(Detection - _detectionDecaySpeed * delta, 0f, 1f);
        else
            _decayTimer += delta;

        // If detection is less than 33%, set the state to default
        if (Detection < 0.33f && DetectionState != EDetectionState.Default)
        {
            DetectionState = EDetectionState.Default;
            OnDetectionStateChanged?.Invoke(DetectionState);
        }
    }

    /// <summary>
    /// Check if the sound is masked by other sounds in heard sounds.
    /// </summary>
    private bool IsMasked(float volume)
    {
        // If there are no heard sounds, return false
        if (_heardSounds.Count == 0)
            return false;

        // If no sound in heard sounds is louder than the given volume, return false
        bool isMasked = false;
        foreach (var sound in _heardSounds)
        {
            // If the sound is louder than the other sounds, return true
            if (sound.Value.Volume > volume)
            {
                isMasked = true;
                break;
            }
        }
        return isMasked;
    }

    private void HandleSoundUpdate(float delta)
    {
        // If we dont have any sounds, return
        if (_heardSounds.Count == 0)
            return;

        // Now we want to update buffer times, durations, and volumes
        // We can use native arrays to make this more performant by storing the index instead of the sound
        List<Sound> soundsToRemove = new List<Sound>();

        for (int i = 0; i < _heardSounds.Count; i++)
        {
            var soundPair = _heardSounds.ElementAt(i);
            var sound = soundPair.Key;
            var soundInfo = soundPair.Value;

            // Updated sound values
            var newVolume = sound.SoundType != Sound.ESoundType.Player ||
                sound.SoundType != Sound.ESoundType.Props ? soundManager.GetSoundVolume(sound, _npc.Position)
                : soundInfo.Volume;
            var newBufferTime = soundInfo.BufferTime - delta;
            var newDuration = newBufferTime <= 0? soundInfo.Duration - delta : soundInfo.Duration;
            var newSoundInfo = new SoundInfo
            {
                Volume = newVolume,
                BufferTime = newBufferTime,
                Duration = newDuration,
                Heard = soundInfo.Heard
            };

            // ___ Looping ___
            // If the sound is looping, we want to update the volume,
            // and remove the sound if the volume is less than 0
            if (sound.Loop)
            {

                // If the volume is less than 0, remove the sound
                if (newVolume <= 0)
                    soundsToRemove.Add(sound);
                // Else update the sound info
                else
                    _heardSounds[sound] = newSoundInfo;
                continue;
            }

            // ___ Player / Props ___
            else if(sound.SoundType == Sound.ESoundType.Player ||
               sound.SoundType == Sound.ESoundType.Props)
            {
                // If the sound is not heard, and buffer time is 0,
                // we want to add detection
                if (!soundInfo.Heard && newBufferTime <= 0)
                {
                    // Check if it is masked
                    if (IsMasked(newVolume))
                    {
                        // If it is masked, remove the sound
                        soundsToRemove.Add(sound);
                        continue;
                    }
                    // Sound should not allow for 100% detection
                    var maxDetection = 0.8f;
                    var clampedVolume = Mathf.Clamp(soundInfo.Volume, 0, maxDetection - Detection);
                    clampedVolume = Mathf.Max(0, clampedVolume); // Make sure it is not negative
                    // Add detection
                    AddDetection(clampedVolume, sound.Pos);
                    // Set the sound as heard
                    newSoundInfo.Heard = true;
                }
                // If the duration is less than 0, remove the sound
                if (newDuration <= 0)
                    soundsToRemove.Add(sound);
                // Else update the sound info
                else
                    _heardSounds[sound] = newSoundInfo;
            }

            // ___ Environment ___
            else if (sound.SoundType == Sound.ESoundType.Environment)
            {
                // If the duration is less than 0, remove the sound
                if (newDuration <= 0)
                    soundsToRemove.Add(sound);
                // Else update the sound info
                else
                    _heardSounds[sound] = newSoundInfo;
            }
        }
        // Remove the sounds
        foreach (var sound in soundsToRemove)
        {
            // Remove the sound from the heard sounds
            _heardSounds.Remove(sound);
        }
    }

    private void HandleVisionUpdate(float delta)
    {
        // If type is sleep, and we are not in alert state, return
        if (_npc.Type == NPC.NPCType.Sleep && DetectionState != EDetectionState.Alert)
            return;
        // If out of range, return
        if (Vector3.Distance(_npc.Position, _player.Position) > 7f)
            return;
        // If the player is outside vision cone, return
        var dirToPlayer = _player.Position - _npc.EyesPos;
        var angle = Vector3.Angle(_npc.transform.forward, dirToPlayer);
        if (angle > MaxPlayerAngle)
            return;

        // If the player is not visible, return
        if (!IsPlayerVisible(out float valueToAdd))
            return;

        // Make it delta time dependent
        valueToAdd *= delta * 0.2f;
        if (valueToAdd <= 0)
            return;
        AddDetection(valueToAdd, _player.Position);
    }
    /// <summary>
    /// Check if the player is visible to the NPC. Output the value of the detection.
    /// </summary>
    private bool IsPlayerVisible(out float value)
    {
        value = 0f;
        value += LimbVisible(_limbs[0]) ? 0.2f : 0f; // Head
        value += LimbVisible(_limbs[1]) ? 0.1f : 0f; // Left Arm
        value += LimbVisible(_limbs[2]) ? 0.1f : 0f; // Right Arm
        value += LimbVisible(_limbs[3]) ? 0.1f : 0f; // Left Leg
        value += LimbVisible(_limbs[4]) ? 0.1f : 0f; // Right Leg
        value += LimbVisible(_limbs[5]) ? 0.4f : 0f; // Torso

        // Brightness Controls multiplier
        value *= _brightnessDetector.CurrentBrightness;
        // If value is not over a threshold, return false
        return value > 0.1f;
    }
    private bool IsPlayerVisible()
    {
        var value = 0f;
        value += LimbVisible(_limbs[0]) ? 0.2f : 0f; // Head
        value += LimbVisible(_limbs[1]) ? 0.1f : 0f; // Left Arm
        value += LimbVisible(_limbs[2]) ? 0.1f : 0f; // Right Arm
        value += LimbVisible(_limbs[3]) ? 0.1f : 0f; // Left Leg
        value += LimbVisible(_limbs[4]) ? 0.1f : 0f; // Right Leg
        value += LimbVisible(_limbs[5]) ? 0.4f : 0f; // Torso

        // Brightness Controls multiplier
        value *= _brightnessDetector.CurrentBrightness;
        // If value is not over a threshold, return false
        return value > 0.1f;
    }
    private bool LimbVisible(Transform limb)
    {
        var lineColor = !Physics.Linecast(_npc.EyesPos, limb.position, _obstacleLayerMask) ? Color.green : Color.red;
        Debug.DrawLine(_npc.EyesPos, limb.position, lineColor);
        return !Physics.Linecast(_npc.EyesPos, limb.position, _obstacleLayerMask);
    }

    /*
    private void DebugHeardSounds()
    {
        // Display the heard sounds, duration, buffer time, and volume
        // Do this in the debug text on NPC
        if (_npc.DebugText == null)
            return;
        _npc.DebugText.text = "Heard Sounds:\n";
        foreach (var soundPair in _heardSounds)
        {
            var sound = soundPair.Key;
            var soundInfo = soundPair.Value;
            _npc.DebugText.text += $"{sound.SoundType} - Buffer Time: {soundInfo.BufferTime} - Duration {soundInfo.Duration}\n";
        }
    }
    */
    // ___ Public Methods ___

    /// <summary>
    /// Update the detector with the given delta time.
    /// </summary>
    public void Update(float delta)
    {
        HandleSoundUpdate(delta);
        HandleDetectionDecay(delta);
        HandleVisionUpdate(delta);
        if (_slider)
            _slider.value = Detection;
    }

    private struct SoundInfo
    {
        public float Volume;
        public float BufferTime;
        public float Duration;
        public bool Heard;
    }

    // Sound and their volumes & buffer times
    private Dictionary<Sound, SoundInfo> _heardSounds = new Dictionary<Sound, SoundInfo>();

    public void OnSoundHeard(Sound sound)
    {
        // If it is a looping sound
        if (_heardSounds.ContainsKey(sound))
            return;

        // Calculate the volume, and remove the sound if it is too low
        var volume = soundManager.GetSoundVolume(sound, _npc.Position);
        if (volume <= 0)
            return;

        var soundInfo = new SoundInfo
        {
            Volume = volume,
            BufferTime = 0.05f,
            Duration = 0.25f
        };

        // If the sound is not made by the player or a prop, set the buffer time to 0
        if (sound.SoundType == Sound.ESoundType.Environment)
            soundInfo.BufferTime = 0f;


        // If there aren't any sounds add it
        if (_heardSounds.Count == 0)
            _heardSounds.Add(sound, soundInfo);
        // Else we wan't to add looping sounds regardless
        else if (sound.Loop)
            _heardSounds.Add(sound, soundInfo);
        // If there is sounds, check if the other sounds are louder (masking)
        else if (IsMasked(volume))
        {
            // If the sound is masked, remove it
            return;
        }
        // Else we want to add the sound
        else
            _heardSounds.Add(sound, soundInfo);

        // Now if a sound is added, we want to make sure any lower sounds
        // are removed from the heard sounds. This is to make sure sounds,
        // with buffer times are removed. Unless this added sound is also a buffer time sound
        if (soundInfo.BufferTime > 0)
            return;
        var soundsToRemove = new List<Sound>();
        foreach (var soundPair in _heardSounds)
        {
            // Dont remove looping sounds
            if (soundPair.Key.Loop)
                continue;
            // If the sound is louder than the other sounds, remove it
            if (soundPair.Value.Volume < volume)
                soundsToRemove.Add(soundPair.Key);
        }
        // Remove the sounds
        foreach (var s in soundsToRemove)
        {
            _heardSounds.Remove(s);
        }
    }

    /// <summary>
    /// Called when the NPC reaches the point of interest.
    /// </summary>
    public void AtPointOfInterest()
    {
        // If the NPC is in alert, we want to set the state to curious
        if (DetectionState == EDetectionState.Alert)
        {
            DetectionState = EDetectionState.Curious;
            OnDetectionStateChanged?.Invoke(DetectionState);
        }
    }
}