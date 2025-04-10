using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class CollisionSoundPlayer : MonoBehaviour
{
    [SerializeField] private Sound.ESoundType _soundType;
    [SerializeField] private float _soundRange = 3f;
    [SerializeField] private float _soundAmplitude = 2f;
    [SerializeField] private AudioClip[] _collisionSound;
    [SerializeField] private float _maxCollisionForce = 50f;
    [SerializeField] private float _minCollisionForce = 0.2f;
    [SerializeField] private float _timeBetweenSounds = 0.1f;
    [SerializeField] private AnimationCurve _volumeCollisionCurve;
    [SerializeField] private AnimationCurve _amplitudeCollisionCurve;
    private float _timer;
    
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = transform.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f; // 3D sound
    }

    private void Update()
    {
        if(_timer > 0f)
            _timer -= Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_minCollisionForce <= collision.relativeVelocity.magnitude && _timer <= 0f)
        {
            _audioSource.clip = _collisionSound[Random.Range(0, _collisionSound.Length)];
            var lerp = collision.relativeVelocity.magnitude / _maxCollisionForce;
            var volume = Mathf.Lerp(0.01f, 0.2f, _volumeCollisionCurve.Evaluate(lerp));
            _audioSource.volume = volume;
            _audioSource.pitch = Random.Range(0.9f, 1.1f);
            
            MakeSound(_amplitudeCollisionCurve.Evaluate(lerp));
            _audioSource.Play();
            _timer = _timeBetweenSounds;
        }
    }

    private void MakeSound(float lerp)
    {
        var volume = Mathf.Lerp(0.1f, _soundAmplitude * 1.5f, lerp);
        var range = Mathf.Lerp(0.1f, _soundRange * 1.5f, lerp);
        var sound = new Sound(transform.position, range, volume)
        {
            SoundType = _soundType
        };
        Sounds.MakeSound(sound);
    }
}
