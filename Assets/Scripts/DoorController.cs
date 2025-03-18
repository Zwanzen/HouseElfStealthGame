using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Transform doorHinge; // Hinge (DoorHinge) transform
    private bool isGrabbed = false; // Sjekker om spilleren kontrollerer d�ren
    public float closeThreshold = 20f; // Grense for automatisk lukking
    public float rotationSpeed = 5f; // Hvor raskt d�ren roteres med musa
    public Transform doorCubeMesh; // Referanse til selve d�r-meshen (DoorCubeMesh)

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isGrabbed = !isGrabbed; // Toggle kontroll
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
        Vector3 newRotation = doorHinge.localEulerAngles; // Bruker Hinge-pivot for rotasjon
        newRotation.y += mouseX * rotationSpeed; // Roterer d�ren p� Y-aksen
        doorHinge.localEulerAngles = newRotation;
    }

    private void CheckClose()
    {
        float angle = Mathf.Abs(Mathf.DeltaAngle(doorCubeMesh.localEulerAngles.y, 0)); // Beregner reell vinkel til lukket posisjon (0)
        if (angle < closeThreshold) // Hvis d�ren er nesten lukket
        {
            Close(); // Lukk d�ren
        }
    }

    private void Close()
    {
        doorCubeMesh.localEulerAngles = new Vector3(0, 0, 0); // Sett d�ren tilbake til lukket posisjon
    }
}
