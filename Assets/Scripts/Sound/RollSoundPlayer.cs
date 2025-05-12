using FMODUnity;
using UnityEngine;

/// <summary>
/// This class is responsible for playing roll sounds,
/// as long as it stays in contact with the ground, and is moving.
/// </summary>
public class RollSoundPlayer : MonoBehaviour
{
    [SerializeField] private EventReference _soundEvent;
    [SerializeField] private float _minVelocity = 0.1f;
    [SerializeField] private float _maxVelocity = 0.1f;
    [SerializeField] private float _bufferTime = 0.1f; // buffer between start/stop


    // Private Variables
    private Rigidbody _rb;
    private StudioEventEmitter _eventEmitter;
    private bool _isSoundPlaying = false;
    private int _collisionCount = 0;

    private float _timer; // buffer between start/stop

    private void Awake()
    {
        // Get the rigidbody component
        _rb = GetComponent<Rigidbody>();
        // Add the FMOD event emitter component
        if (_eventEmitter == null)
        {
            _eventEmitter = gameObject.AddComponent<StudioEventEmitter>();
            _eventEmitter.EventReference = _soundEvent;
        }

    }

    private void Update()
    {
        var shouldPlaySound = IsMoving() && _collisionCount > 0;
        if (!shouldPlaySound)
            _timer += Time.deltaTime;
        else
            _timer = 0f;

        if (shouldPlaySound && !_isSoundPlaying)
        {
            PlayLoopingSound();
            _isSoundPlaying = true;
        }
        else if (!shouldPlaySound && _isSoundPlaying && _timer > _bufferTime)
        {
            StopLoopingSound();
            _isSoundPlaying = false;
        }

        // Update audio properties
        if (_isSoundPlaying)
        {
            // Calculate the speed of the object
            var speed = _rb.linearVelocity.magnitude;
            // Map the speed to a range between 0 and 1
            var normalizedSpeed = Mathf.Clamp01(speed / _maxVelocity);
            // Set the parameter in FMOD
            _eventEmitter.SetParameter("Magnitude", normalizedSpeed);
        }
    }

    private bool IsMoving()
    {
        // Check if the rigidbody is moving
        return _rb != null && _rb.linearVelocity.magnitude > _minVelocity;
    }

    /// <summary>
    /// Plays once the rolling starts.
    /// </summary>
    private void PlayLoopingSound()
    {
        _eventEmitter.Play();
    }

    /// <summary>
    /// Stops once the rolling stops.
    /// </summary>
    private void StopLoopingSound()
    {
        _eventEmitter.Stop();
    }

    // Keep track of the number of collisions
    private void OnCollisionEnter(Collision collision)
    {
        _collisionCount++;
    }

    private void OnCollisionExit(Collision collision)
    {
        _collisionCount--;
        // Ensure count doesn't go below zero (safety check)
        if (_collisionCount < 0)
            _collisionCount = 0;
    }
}
