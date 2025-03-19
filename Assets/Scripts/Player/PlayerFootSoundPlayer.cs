using System;
using UnityEngine;

public class PlayerFootSoundPlayer : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] PlayerController _player;
    
    [Space(10f)]
    [Header("Settings")]
    [SerializeField] private float _minVolume = 0.5f;
    [SerializeField] private float _maxVolume = 1f;
    [SerializeField] private float _minPitch = 0.8f;
    [SerializeField] private float _maxPitch = 1.2f;
    
    [Space(10f)]
    [Header("Foot Sounds")]
    [SerializeField] private AudioClip[] _woodFootSounds;
    [SerializeField] private AudioClip[] _metalFootSounds;
    [SerializeField] private AudioClip[] _carpetFootSounds;
    
    public enum EFootSoundType
    {
        Wood,
        Metal,
        Carpet
    }
    
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public void PlayFootSound(EFootSoundType footSoundType)
    {
        var audioClip = _woodFootSounds[0];
        var range = 0f;
        var amplitude = 1f;
        if (footSoundType == EFootSoundType.Wood)
        {
            audioClip = _woodFootSounds[UnityEngine.Random.Range(0, _woodFootSounds.Length)];
            range = 3f;
            amplitude = 5f;
        }
        else if (footSoundType == EFootSoundType.Metal)
        {
            audioClip = _metalFootSounds[UnityEngine.Random.Range(0, _metalFootSounds.Length)];
            range = 6f;
            amplitude = 10f;
        }
        else if (footSoundType == EFootSoundType.Carpet)
        {
            audioClip = _carpetFootSounds[UnityEngine.Random.Range(0, _carpetFootSounds.Length)];
            range = 0.3f;
            amplitude = 1f;
        }
        
        // Calculate volume modifier based on current player speed
        var playerSpeed = _player.CurrentPlayerSpeed;
        var speedLerp = ((float)playerSpeed - 1f) / 3f; // Assuming 3 is the max speed for the player SNEAKING
        if (speedLerp > 1)
        {
            speedLerp = 1;
        }
        
        // Calculate the volume based on the speed modifier
        var newMin = Mathf.Lerp(_minVolume / 2f, _minVolume, speedLerp);
        var newMax = Mathf.Lerp(_maxVolume / 2f, _maxVolume, speedLerp);
        var randomVolume = UnityEngine.Random.Range(newMin, newMax);
        
        
        // Calculate new range and amplitude based on speed
        range += (range/2) * speedLerp;      
        amplitude += (amplitude/2) * speedLerp;
        
        _audioSource.clip = audioClip;
        _audioSource.volume = randomVolume;
        _audioSource.pitch = UnityEngine.Random.Range(_minPitch, _maxPitch);
        _audioSource.Play();
        var sound = new Sound(transform.position, range, amplitude)
        {
            SoundType = Sound.ESoundType.Player
        };
        Sounds.MakeSound(sound);
    }
    
}
