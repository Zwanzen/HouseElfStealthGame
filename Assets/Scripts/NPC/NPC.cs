using System;
using Pathfinding;
using UnityEngine;
using static RigidbodyMovement;

[RequireComponent(typeof(Rigidbody), typeof(Seeker))]
public class NPC : MonoBehaviour
{
    public Transform Target;
    
    [Header("NPC Settings")]
    [SerializeField] private NPCType _npcType;
    [SerializeField] private NPCPath _path;
    
    [Space(10)]
    [Header("Movement Settings")]
    [SerializeField] private float _lookAhead = 1f;
    [SerializeField] private MovementSettings _settings;
    [SerializeField] private float _rotationSpeed = 100f;

    
    private Rigidbody _rigidbody;
    private NPCMovement _movement;
    private enum NPCType
    {
        Patrol,
    }
    
    
    // Properties
    public Rigidbody Rigidbody => _rigidbody;
    public MovementSettings MovementSettings => _settings;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _movement = new NPCMovement(this, _lookAhead, _rotationSpeed);
        
        _movement.ArrivedAtTarget += OnReachedTarget;
    }

    private void Start()
    {
        _movement.SetTarget(_path);
    }

    private void Update()
    {
        // If we press the space key, we will move to the target position
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Go();
        }
    }

    private void FixedUpdate()
    {
        var delta = Time.fixedDeltaTime;
        _movement.FixedUpdate(delta);
    }

    private void Go()
    {
        _movement.SetTarget(Target.position);
    }
    
    private void OnReachedTarget()
    {
        _movement.SetTarget(_path);
    }
}
