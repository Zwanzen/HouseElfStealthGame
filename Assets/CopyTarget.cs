using UnityEngine;

/// <summary>
/// Places this transform at the same position and rotation as the target transform,
/// while maintaining an initial rotation offset.
/// </summary>
public class CopyTarget : MonoBehaviour
{
    [SerializeField] private Transform _target;

    private Quaternion _initialOffset;

    private void Awake()
    {
        // Calculate the initial rotation offset between this transform and the target
        // This preserves the initial difference in rotation
        _initialOffset = transform.rotation * Quaternion.Inverse(_target.rotation);
    }

    private void FixedUpdate()
    {
        // Copy the target position
        transform.position = _target.position;

        // Apply the target's rotation with our initial offset
        transform.rotation = _initialOffset * _target.rotation;
    }
}
