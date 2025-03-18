using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Transform hinge; // Rotasjonspunktet for d�ren
    private bool isGrabbed = false; // Sjekker om spilleren kontrollerer d�ren
    private Quaternion closedRotation; // Startrotasjonen til d�ren
    private float closeThreshold = 10f; // Grense for automatisk lukking
    public float rotationSpeed = 5f; // Hvor raskt d�ren roteres med musa
    [SerializeField] private Rigidbody _rigidbody;

    //Read only properties
    public Rigidbody Rigidbody => _rigidbody;

    private void Start()
    {
        closedRotation = Quaternion.Euler(0, 0, 0); // Lukket posisjon = 0 grader
    }

    private void Update()
    {
        // Trykk F for � toggle d�rkontroll
        if (Input.GetKeyDown(KeyCode.F))
        {
            isGrabbed = !isGrabbed; // Bytter mellom � kontrollere eller ikke
        }

        if (isGrabbed)
        {
            HandleDoor(); // Lar spilleren rotere d�ren med musa
        }
        else
        {
            CheckClose(); // Sjekker om d�ren skal lukkes automatisk
        }
    }

    private void HandleDoor()
    {
        float mouseX = Input.GetAxis("Mouse X"); // Henter musebevegelse horisontalt
        Vector3 newRotation = hinge.localEulerAngles;
        newRotation.y += mouseX * rotationSpeed; // Roterer kun rundt Y-aksen
        hinge.localEulerAngles = newRotation;
    }

    private void CheckClose()
    {
        float angle = Mathf.Abs(Mathf.DeltaAngle(hinge.localEulerAngles.y, 0)); // Beregner reell vinkel til 0 (lukket)
        if (angle < closeThreshold) // Hvis d�ren er nesten lukket
        {
            Close(); // Lukk d�ren
        }
    }

    private void Close()
    {
        hinge.rotation = closedRotation; // Setter d�ren tilbake til startposisjon
    }
}
