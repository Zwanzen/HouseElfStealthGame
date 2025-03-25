using System;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class CollisionSoundPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] _collisionSound;
    [SerializeField] private float _collisionThreshold = 1.5f;
    [SerializeField] private float _timeBetweenSounds = 0.1f;
    private float _timer;
    
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = transform.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if(_timer > 0f)
            _timer -= Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_collisionThreshold <= collision.relativeVelocity.magnitude && _timer <= 0f)
        {
            _audioSource.clip = _collisionSound[Random.Range(0, _collisionSound.Length)];
            var volume = Mathf.Lerp(0.01f, 0.16f, collision.relativeVelocity.magnitude / _collisionThreshold * 10f);
            Debug.Log(collision.relativeVelocity.magnitude);
            _audioSource.volume = volume;
            _audioSource.pitch = Random.Range(0.9f, 1.1f);
            _audioSource.Play();
            _timer = _timeBetweenSounds;
        }
    }
}
