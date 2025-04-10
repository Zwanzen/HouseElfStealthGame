using UnityEngine;
public class CustomAnchorPoint : MonoBehaviour
{
    [Header("Custom Anchor Point")]
    [SerializeField] private bool _useCustomAnchorPoint = false;
    [SerializeField] private ConfigurableJoint _anchorJoint;
    [SerializeField] private Transform _anchorTarget;
    
    [Space(10f)]
    [Header("Custom Connected Anchor Point")]
    [SerializeField] private bool _useCustomConnectedAnchorPoint = false;
    [SerializeField] private ConfigurableJoint _connectedJoint;
    [SerializeField] private Transform _connectedTarget;

    private Vector3 AnchorPosition => GetAnchorPosition();
    private Vector3 ConnectedTargetPosition => GetConnectedAnchorPosition();
    
    private void Awake()
    {
        if(_useCustomConnectedAnchorPoint)
            _connectedJoint.autoConfigureConnectedAnchor = false;
        
        if (_useCustomAnchorPoint)
            _anchorJoint.anchor = AnchorPosition;
            
    }

    private void FixedUpdate()
    {
        if (_useCustomConnectedAnchorPoint)
            _connectedJoint.connectedAnchor = ConnectedTargetPosition;
    }

    private Vector3 GetAnchorPosition()
    {
        var anchorPosition = _anchorJoint.transform.InverseTransformPoint(_anchorTarget.position);

        return anchorPosition;
    }
    
    private Vector3 GetConnectedAnchorPosition()
    {
        var targetPositionWorld = _connectedTarget.position;
        var connectedBody = _connectedJoint.connectedBody;
        var targetConnectedAnchorPosition = connectedBody != null ? 
            connectedBody.transform.InverseTransformPoint(targetPositionWorld) : targetPositionWorld;
        return targetConnectedAnchorPosition;
    }
}
