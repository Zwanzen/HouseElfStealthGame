using UnityEngine;

public class FootCollisionController : MonoBehaviour
{

    [SerializeField] private float _forceMultiplier = 1f;
    [SerializeField] private float _maxForce = 5f;
    [SerializeField] private LayerMask _objectLayer;
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Object"))
        {
            Rigidbody rb = collision.rigidbody;
            if (rb != null)
            {
                var dir = (transform.position - rb.position);
                dir = Vector3.up + dir.normalized;
                var relativeForce = _forceMultiplier * (collision.relativeVelocity.magnitude);
                relativeForce += _forceMultiplier;
                relativeForce = Mathf.Clamp(relativeForce, 0f, _maxForce);
                
                rb.AddForce(-dir.normalized * relativeForce, ForceMode.Impulse);
            }
        }
    }
}
