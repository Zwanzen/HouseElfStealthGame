using System.Collections.Generic;
using UnityEngine.Rendering;

public class NPCDetector
{
    private NPC npc;
    private SoundGameplayManager soundManager;

    public NPCDetector(NPC npc)
    {
        this.npc = npc;
        soundManager = SoundGameplayManager.Instance;
    }

    // Properties
    /// <summary>
    /// The current detection level of the NPC.
    /// </summary>
    public float Detection { get; private set; }

    private List<Sound> _storedSounds;
    private List<Sound> _soundsToDetect;
    public void OnSoundHeard(Sound sound)
    {
        // If it is a looping sound
        if (_storedSounds.Contains(sound))
            return;

        // Calculate the volume, and remove the sound if it is too low
        var volume = soundManager.GetSoundVolume(sound, npc.Position);
        if (volume <= 0)
            return;


    }

    /// <summary>
    /// Update the detector with the given delta time.
    /// </summary>
    public void Update(float delta)
    {
        HandleSoundUpdate(delta);
    }


    private void HandleSoundUpdate(float delta)
    {

    }
}