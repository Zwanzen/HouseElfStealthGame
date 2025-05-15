using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CollisionSoundPlayer : MonoBehaviour
{
    [SerializeField] private EventReference _soundEvent;
    [SerializeField] private Sound.ESoundType _soundType = Sound.ESoundType.Props;
    [SerializeField] private float _soundRange = 7f;
    [SerializeField] private float _soundAmplitude = 7f;
    [SerializeField] private float _maxCollisionForce = 3f;
    [SerializeField] private float _minCollisionForce = 0.2f;
    [SerializeField, Range(0f,1f)] private float _minMagnitude = 0.1f;
    [SerializeField] private float _timeBetweenSounds = 0.1f;
    private float _timer;
    
    private StudioEventEmitter _eventEmitter;

    // For startup delay
    private float _startDelay = 3f;
    private float _startTimer;


    private void Awake()
    {
        if(_eventEmitter == null)
        {
            _eventEmitter = gameObject.AddComponent<StudioEventEmitter>();
            _eventEmitter.EventReference = _soundEvent;
        }
    }

    private void Update()
    {
        if(_timer > 0f)
            _timer -= Time.deltaTime;

        if(_startTimer < _startDelay)
            _startTimer += Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only play sound after a delay (Game Start)
        if (_startTimer < _startDelay)
            return;

        if (_minCollisionForce <= collision.relativeVelocity.magnitude && _timer <= 0f)
        {
            var relMag = collision.relativeVelocity.magnitude / _maxCollisionForce;
            MakeSound(relMag);
            _eventEmitter.Play();
            _eventEmitter.SetParameter("Magnitude", Mathf.Clamp(relMag, _minMagnitude, 1f));
            _timer = _timeBetweenSounds;
        }
    }

    private void MakeSound(float lerp)
    {
        var volume = Mathf.Lerp(0.1f, _soundAmplitude, lerp);
        var range = Mathf.Lerp(0.1f, _soundRange, lerp);
        var sound = new Sound(transform.position, range, volume)
        {
            SoundType = _soundType
        };
        Sounds.MakeSound(sound);
    }
}
