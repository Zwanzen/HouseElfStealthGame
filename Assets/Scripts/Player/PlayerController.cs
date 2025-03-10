using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    // State Machines
    private PlayerControlStateMachine _controlStateMachine;
    
    // State Machine Contexts
    private PlayerControlContext _controlContext;
    
    // Private variables
    [Header("Components")]
    [SerializeField] private Rigidbody _rigidbody;
    
    [Space(10f)]
    [Header("Common Variables")]
    [SerializeField] private LayerMask _groundLayers;
    
    [Space(10f)]
    [Header("Control Variables")]
    [SerializeField] private float _springStrength = 250f;
    [SerializeField] private float _springDampener = 5f;
    

    private void Awake()
    {
        // Initialize the state machine contexts first, SMs depend on them
        InitializeStateMachineContexts();
        InitializeStateMachines();
    }

    private void Update()
    {
    }

    // Initialize the state machine contexts
    private void InitializeStateMachineContexts()
    {
        _controlContext = new PlayerControlContext(this, _controlStateMachine, _rigidbody, _groundLayers,
            _springStrength, _springDampener);
        
    }
    

    // Adding and initializing the state machines with contexts
    private void InitializeStateMachines()
    {
        // Add the state machines to the player controller
        _controlStateMachine = this.AddComponent<PlayerControlStateMachine>();
        
        // Set the context for the state machines
        _controlStateMachine.SetContext(_controlContext);
    }
    
    // Read-only properties
    public static float Height => 1.0f;
    
    /// <summary>
    /// The player's offset position from the rigidbody's position, adjusted by the character height.
    /// The player rigidbody is actually at the feet of the player, so we need to add the character height to the position.
    /// </summary>
    public Vector3 Position => _rigidbody.position + new Vector3(0, Height, 0); 
    
    /// <summary>
    /// The threshold for the height difference to consider the player grounded.
    /// </summary>
    public static float HeightThreshold => 0.07f;
    
    /// <summary>
    /// If the player's control state is grounded.
    /// </summary>
    public bool IsGrounded => _controlStateMachine.State == PlayerControlStateMachine.EPlayerControlState.Grounded;
    
}
