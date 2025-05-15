using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    private PlayerController player;
    private void Start()
    {
        player = PlayerController.Instance;
    }

    private void Update()
    {
        if (player != null)
        {
            Vector3 direction = player.Position - transform.position;
            direction.y = 0; // Keep the rotation on the Y axis only
            transform.rotation = Quaternion.LookRotation(direction);

        }
    }
}
