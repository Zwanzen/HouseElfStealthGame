using System;
using UnityEngine;

public class LoopingSoundMaker : MonoBehaviour
{

    [SerializeField] private float _amplitude = 100f;
    [SerializeField] private float _range = 10f;
    private AudioSource _audioSource;
    [SerializeField] private float frequency = 1f;
    private float _soundTimer;
    private float _makeSoundTimer;
    private Sound _loopingSound;

    private bool _isLoud;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        AudioStart();
    }

    private void Update()
    {
        if (_isLoud)
        {
            _soundTimer += Time.deltaTime;
            if(_soundTimer >= 30f)
            {
                _loopingSound.Amplitude = 0f;
                _isLoud = false;
            }
            MakeSound();
        }
    }

    private void FixedUpdate()
    {
        if (!_audioSource.isPlaying)
        {
            AudioStart();
        }
    }

    private void AudioStart()
    {
        // Start making sound when the audio starts
        _loopingSound = new Sound(transform.position, _range, _amplitude, true);
        _soundTimer = 0f;
        _isLoud = true;
        _audioSource.Play();
    }



    private void MakeSound()
    {
        _makeSoundTimer += Time.deltaTime;
        if(frequency > _makeSoundTimer)
            return;
        _makeSoundTimer = 0f;
        
        // Update the position of the looping sound
        _loopingSound.UpdateSoundPosition(transform.position);
        
        // Make the sound
        Sounds.MakeSound(_loopingSound);
    }
}
