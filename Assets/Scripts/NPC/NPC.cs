using System;
using Pathfinding;
using UnityEngine;
using static RigidbodyMovement;

[RequireComponent(typeof(Rigidbody), typeof(Seeker), typeof(NPCMovement))]
public class NPC : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private NPCType _npcType;
    [SerializeField] private MovementSettings _movementSettings;
    [SerializeField] private NPCPath _path;
    private enum NPCType
    {
        Patrol,
    }
    
    private Rigidbody _rigidbody;
    private NPCMovement _npcMovement;
    
    // Properties
    public Rigidbody Rigidbody => _rigidbody;
    public MovementSettings MovementSettings => _movementSettings;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _npcMovement = GetComponent<NPCMovement>();
    }

    private void Start()
    {

    }

    float _timer = 0;
    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer > 1)
        {
            _timer = 0;
            Go();
        }
    }

    private void Go()
    {
        _npcMovement.SetTarget(_path);
    }
}
