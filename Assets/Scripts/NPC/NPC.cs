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
    }

    private void Start()
    {
        _movement.SetTarget(_path);
    }

    private void Update()
    {
        _movement.Update(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        _movement.HandleMovement();
    }

    private void Go()
    {
        _movement.SetTarget(Target.position);
    }
}
