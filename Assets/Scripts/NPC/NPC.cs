using System;
using Pathfinding;
using UnityEngine;
using static RigidbodyMovement;

[RequireComponent(typeof(Rigidbody), typeof(Seeker))]
public class NPC : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private NPCType _npcType;
    [SerializeField] private NPCPath _path;

    [Space(10)] [Header("Movement Settings")] [SerializeField]
    private float _maxRecalcPathTime = 0.5f;
    [SerializeField] private float _lookAhead = 1f;
    [Space(5)]
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField] private float _springStrength = 50f;
    [SerializeField] private float _springDamper = 5f;
    [SerializeField] private MovementSettings _settings;
    [SerializeField] private float _rotationSpeed = 100f;

    // ___ Components ___
    private Rigidbody _rigidbody;
    private Animator _anim;
    
    // ___ NPC Specific ___
    private NPCMovement _movement;
    NPCAnimator _animator;
    // NPCDetector _detector;
    
    private enum NPCType
    {
        Patrol,
        Sleep,
        Work,
    }
    
    private enum State
    {
        Default, // Does his job, patrols, etc.
        Curious, // Will stop and look around
        Alert, // Will look for the player
    }
    
    
    // Properties
    public Rigidbody Rigidbody => _rigidbody;
    public MovementSettings MovementSettings => _settings;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _anim = GetComponentInChildren<Animator>();
        
        _movement = new NPCMovement(this,_maxRecalcPathTime, _lookAhead, _groundLayers, _springStrength, _springDamper, _rotationSpeed);
        _animator = new NPCAnimator(this, _anim);
        
        
        _movement.ArrivedAtTarget += OnReachedTarget;
        _movement.OnAnimStateChange += OnAnimStateChange;
    }

    private void Start()
    {
        _movement.SetTarget(_path);
    }

    private void Update()
    {
        var delta = Time.deltaTime;
        _animator.Update(delta);
    }

    private void FixedUpdate()
    {
        var delta = Time.fixedDeltaTime;
        _movement.FixedUpdate(delta);
    }
    
    private void OnReachedTarget()
    {
        _movement.SetTarget(_path);
    }

    private void OnAnimStateChange(NPCAnimator.AnimState state)
    {
        _animator.SetNewAnimState(state);
    }
}
