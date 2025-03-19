

using System;
using UnityEngine;

public class LoopingSoundPlayer : MonoBehaviour
{
        private AudioSource _audioSource;
        private Sound _sound;

        private float timer = 10f;
        
        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                timer = 10f;

                _sound = new Sound(transform.position, 100f);
                _sound.SoundType = Sound.ESoundType.Environment;
                
                Sounds.MakeLoopingSound(this, _sound);
            }
            
            if (!_audioSource.isPlaying)
            {
                Sounds.StopLoopingSound(this, _sound);
            }
        }
}