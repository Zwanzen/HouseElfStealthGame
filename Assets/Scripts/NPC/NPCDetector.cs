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

    private List<Sound> _heardSounds;
    private List<Sound> _soundsToDetect;
    public void OnSoundHeard(Sound sound)
    {
        var volume = soundManager.GetSoundVolume(sound, npc.Position);
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