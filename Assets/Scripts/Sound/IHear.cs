public interface IHear
{
    void RespondToSound(Sound sound);
    
    void RespondToLoopingSound(LoopingSoundPlayer player, Sound sound);
    
    void StopRespondToLoopingSound(LoopingSoundPlayer player, Sound sound);
}
