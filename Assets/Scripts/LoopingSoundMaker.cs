using System;
using UnityEngine;

public class LoopingSoundMaker : MonoBehaviour
{

    
    [SerializeField] private float frequency = 1f;
    private float timer;
    private Sound _loopingSound;

    private void Awake()
    {
        // Initialize the looping sound
        _loopingSound = new Sound(transform.position, 10f, 5f, true);
    }
    
    private void Update()
    {
        timer += Time.deltaTime;
        if(frequency > timer)
            return;
        timer = 0f;
        
        // Update the position of the looping sound
        _loopingSound.UpdateSoundPosition(transform.position);
        
        // Make the sound
        Sounds.MakeSound(_loopingSound);
    }
}
