using System;
using Pathfinding;
using UnityEngine;

[RequireComponent(typeof(Seeker))]
public class AstarTest : MonoBehaviour
{
    [SerializeField] private Transform _target;
    private float _timer;

    private Seeker _seeker;

    private void Awake()
    {
        _seeker = GetComponent<Seeker>();   
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            _timer = 1f;
            Debug.Log("Getting path");
            
            // Use A* pathfinding to get a path from this to the target
            _seeker.StartPath(transform.position, _target.position, OnPathComplete);
            
        }
    }

    private void OnPathComplete(Path path)
    {
        // The path is now calculated!

    }
}
