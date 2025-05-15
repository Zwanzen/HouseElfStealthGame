using System;
using UnityEngine;

public class SoundBroadcaster : IHear
{
    public void RespondToSound(Sound sound)
    {
        OnSoundBroadcasted?.Invoke(sound);
    }

    public Action<Sound> OnSoundBroadcasted;
}
