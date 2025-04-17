using System;
using Pathfinding;
using UnityEngine;
using static RigidbodyMovement;

[RequireComponent(typeof(Rigidbody), typeof(Seeker))]
public class NPC : MonoBehaviour
{
    public Transform Target;
    public float LookAhead = 1f;
    
    [Header("NPC Settings")]
    [SerializeField] private NPCType _npcType;
    [SerializeField] private MovementSettings _movementSettings;
    [SerializeField] private NPCPath _path;
    
    private Rigidbody _rigidbody;
    private NPCMovement _movement;
    private enum NPCType
    {
        Patrol,
    }
    
    
    // Properties
    public Rigidbody Rigidbody => _rigidbody;
    public MovementSettings MovementSettings => _movementSettings;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _movement = new NPCMovement(this, LookAhead);
        
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
        _movement.HandleMovement(delta);
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
