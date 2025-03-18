using System;
using UnityEngine;

public class PlayerFootSoundPlayer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _minVolume = 0.5f;
    [SerializeField] private float _maxVolume = 1f;
    [SerializeField] private float _minPitch = 0.8f;
    [SerializeField] private float _maxPitch = 1.2f;
    
    [Space(10f)]
    [Header("Foot Sounds")]
    [SerializeField] private AudioClip[] _woodFootSounds;
    
    public enum EFootSoundType
    {
        Wood,
    }
    
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public void PlayFootSound(EFootSoundType footSoundType)
    {
        AudioClip audioClip = _woodFootSounds[0];
        if (footSoundType == EFootSoundType.Wood)
        {
            audioClip = _woodFootSounds[UnityEngine.Random.Range(0, _woodFootSounds.Length)];
        }
        
        _audioSource.clip = audioClip;
        _audioSource.volume = UnityEngine.Random.Range(_minVolume, _maxVolume);
        _audioSource.pitch = UnityEngine.Random.Range(_minPitch, _maxPitch);
        _audioSource.Play();
        Sound sound = new Sound(transform.position, 5f);
        sound.SoundType = Sound.ESoundType.Player;
        Sounds.MakeSound(sound);
    }
    
}
