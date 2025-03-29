using RootMotion.FinalIK;
using UnityEngine;

public class FootControlContext
{
        
    [Header("Components")]
    private PlayerController _player;
    private FullBodyBipedIK _bodyIK;
    private PlayerFootSoundPlayer _footSoundPlayer;
    
    [Header("Common")]
    private LayerMask _groundLayers;
    private Rigidbody _footTarget;
    private Rigidbody _otherFootTarget;
    private Transform _footRestTarget;
    
    [Header("Foot Control Variables")]
    private float _stepLength;
    private float _stepHeight;
    
    public FootControlContext()
    {
        
    }
}