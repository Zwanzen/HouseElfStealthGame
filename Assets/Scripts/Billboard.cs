using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform _cameraTransform;
    private void Awake()
    {
        _cameraTransform = Camera.main.transform;
    }
    
    private void Update()
    {
        transform.LookAt(_cameraTransform.position, Vector3.up);
        transform.Rotate(0f, 180f, 0f);
    }
}
