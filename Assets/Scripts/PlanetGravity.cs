using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlanetGravity : MonoBehaviour
{
    public Transform planetCenter;    // Reference to the planet's center
    public float gravityStrength = 9.8f; // Strength of the gravitational pull

    private Rigidbody rb;

    void Start() {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable default gravity
        //rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent rigidbody from tumbling randomly
    }

    void FixedUpdate() {
        ApplyGravity();
    }

    private void ApplyGravity() {
        // Calculate direction towards the planet's center
        Vector3 directionToCenter = (planetCenter.position - transform.position).normalized;

        // Apply custom gravity force
        rb.AddForce(directionToCenter * gravityStrength, ForceMode.Acceleration);
    }
}
