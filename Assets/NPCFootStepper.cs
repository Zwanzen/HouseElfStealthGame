using System;
using UnityEngine;

public class NPCFootStepper : MonoBehaviour
{

    [SerializeField] private AudioClip[] _footstepSounds;
    [SerializeField] private AnimationCurve _footPositionCurve;
    [SerializeField] private float _footStepFrequency = 1f;
    private float _footStepTimer = 0f;
    
    private AudioSource _audioSource;
    private Vector3 _startPosition;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _startPosition = transform.position;
    }

    private void Update()
    {
        _footStepTimer += Time.deltaTime * _footStepFrequency;
        
        transform.position = _startPosition + Vector3.up * _footPositionCurve.Evaluate(_footStepTimer);
        
        if (_footStepTimer >= 1f)
        {
            _footStepTimer = 0f;
            PlayStepSound();
        }
    }
    
    private void PlayStepSound()
    {
        var sound = new Sound(transform.position, 5, 20)
        {
            Duration = 0.2f
        };
        Sounds.MakeSound(sound);
        _audioSource.clip = _footstepSounds[UnityEngine.Random.Range(0, _footstepSounds.Length)];
        _audioSource.Play();
    }
}
