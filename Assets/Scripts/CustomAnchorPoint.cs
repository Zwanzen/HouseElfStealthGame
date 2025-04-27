using UnityEditor.Experimental.GraphView;
using UnityEngine;
public class CustomAnchorPoint : MonoBehaviour
{
    [Header("Custom Anchor Point")]
    [SerializeField] private bool _useCustomAnchorPoint = false;
    [SerializeField] private ConfigurableJoint _anchorJoint;
    [SerializeField] private Transform _anchorTarget;
    [SerializeField] private bool _updateAnchorPosition = false;
    [SerializeField] private Transform _anchorPoleDirectionTarget;

    [Space(10f)]
    [Header("Custom Connected Anchor Point")]
    [SerializeField] private bool _useCustomConnectedAnchorPoint = false;
    [SerializeField] private ConfigurableJoint _connectedJoint;
    [SerializeField] private Transform _connectedTarget;

    private Vector3 AnchorPosition => GetAnchorPosition();
    private Vector3 AnchorPoleDirection => _anchorPoleDirectionTarget ? _anchorPoleDirectionTarget.position - _anchorJoint.targetPosition : AnchorPosition.normalized;
    private Vector3 ConnectedTargetPosition => GetConnectedAnchorPosition();

    private void Awake()
    {
        if (_useCustomConnectedAnchorPoint)
            _connectedJoint.autoConfigureConnectedAnchor = false;

        if (_useCustomAnchorPoint)
            _anchorJoint.anchor = AnchorPosition;
    }

    private void FixedUpdate()
    {
        if (_useCustomConnectedAnchorPoint)
            _connectedJoint.connectedAnchor = ConnectedTargetPosition;

        if (_useCustomAnchorPoint && _updateAnchorPosition)
            _anchorJoint.anchor = GetPoledAnchorPosition();
    }

    private Vector3 GetAnchorPosition()
    {
        var anchorPosition = _anchorJoint.transform.InverseTransformPoint(_anchorTarget.position);

        return anchorPosition;
    }

    /// <summary>
    /// This returns the anchor position along a pole from the anchor target.
    /// This stops the joint from stretching in the direction of the pole.
    /// </summary>
    private Vector3 GetPoledAnchorPosition()
    {
        // We need the anchor point in world space
        var anchorPositionWorld = _anchorJoint.transform.TransformPoint(_anchorJoint.anchor);

        // We need the direction from the anchor point to the target
        var direction = _anchorTarget.position - anchorPositionWorld;
        // We need the direction of the pole
        var poleDirection = _anchorJoint.transform.TransformDirection(AnchorPoleDirection);


        // If we want the anchor point to be along the pole closest to the target
        // we need to get the projection of the direction onto the pole direction
        var projection = Vector3.Project(direction, poleDirection);


        // Now we can add the projection to the anchor point
        var anchorPosition = anchorPositionWorld + projection;
        // We need to transform the anchor position back to local space
        var anchorPositionLocal = _anchorJoint.transform.InverseTransformPoint(anchorPosition);
        return anchorPositionLocal;
    }

    private Vector3 GetConnectedAnchorPosition()
    {
        var targetPositionWorld = _connectedTarget.position;
        var connectedBody = _connectedJoint.connectedBody;
        var targetConnectedAnchorPosition = connectedBody != null ?
            connectedBody.transform.InverseTransformPoint(targetPositionWorld) : targetPositionWorld;
        return targetConnectedAnchorPosition;
    }


    private void OnDrawGizmosSelected()
    {
        if(!Application.isPlaying)
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_anchorJoint.transform.TransformPoint(_anchorJoint.anchor), 0.025f);
        Gizmos.color = Color.blue;
        if (_connectedJoint.connectedBody)
        {
            Gizmos.DrawSphere(_connectedJoint.connectedBody.transform.TransformPoint(_connectedJoint.connectedAnchor), 0.03f);
        }
        else
        {
            Gizmos.DrawSphere(_connectedJoint.transform.TransformPoint(_connectedJoint.connectedAnchor), 0.03f);
        }
        // Anchor Pole Direction

        if (!_updateAnchorPosition)
            return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(_anchorJoint.transform.TransformPoint(_anchorJoint.anchor), _anchorJoint.transform.TransformPoint(_anchorJoint.anchor) + _anchorJoint.transform.TransformDirection(AnchorPoleDirection) * 0.5f);
    }
}
