using UnityEngine;

public static class Sounds
{

    public static void MakeSound(Sound sound)
    {

        Collider[] col = Physics.OverlapSphere(sound.Pos, sound.Range);
        
        for (int i = 0; i < col.Length; i++)
            if(col[i].TryGetComponent(out IHear hear))
                hear.RespondToSound(sound);
    }
    
    public static void MakeLoopingSound(LoopingSoundPlayer player,Sound sound)
    {
        Collider[] col = Physics.OverlapSphere(sound.Pos, sound.Range);
        
        for (int i = 0; i < col.Length; i++)
            if(col[i].TryGetComponent(out IHear hear))
                hear.RespondToLoopingSound(player, sound);
    }
    
    public static void StopLoopingSound(LoopingSoundPlayer player, Sound sound)
    {
        Collider[] col = Physics.OverlapSphere(sound.Pos, sound.Range);
        
        for (int i = 0; i < col.Length; i++)
            if(col[i].TryGetComponent(out IHear hear))
                hear.StopRespondToLoopingSound(player ,sound);
    }
    
}
