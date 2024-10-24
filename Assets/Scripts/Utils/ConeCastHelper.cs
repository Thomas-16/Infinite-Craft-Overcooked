using UnityEngine;
using System.Collections.Generic;

public class ConeCastHelper
{
    private List<Vector3> rayDirections;  // Store the precomputed ray directions

    public void InitializeConeCast(float coneAngle, int numRays) {
        // Initialize the list to store ray directions
        rayDirections = new List<Vector3>();

        // Precompute the ray directions using spherical coordinates
        for (int i = 0; i < numRays; i++) {
            float theta = Random.Range(0f, 2f * Mathf.PI); // Azimuth angle (0 to 360 degrees)
            float phi = Mathf.Acos(Random.Range(Mathf.Cos(Mathf.Deg2Rad * coneAngle), 1f)); // Polar angle within the cone

            // Store the spherical angles to compute ray directions later
            rayDirections.Add(new Vector3(phi, theta, 0f));  // Store spherical coords for later conversion
        }
    }

    public RaycastHit[] ConeCast(Vector3 origin, Vector3 direction, float maxDistance) {
        List<RaycastHit> hits = new List<RaycastHit>();

        // Normalize the direction vector
        direction.Normalize();

        // Use precomputed spherical coordinates to cast rays in the given direction
        foreach (Vector3 spherical in rayDirections) {
            float phi = spherical.x;
            float theta = spherical.y;

            // Convert the spherical coordinates to Cartesian using the current direction
            Vector3 rayDirection = SphericalToCartesian(phi, theta, direction);

            RaycastHit hit;
            if (Physics.Raycast(origin, rayDirection, out hit, maxDistance)) {
                hits.Add(hit);
            }
        }

        // Return the results of all raycasts
        return hits.ToArray();
    }

    // Converts spherical coordinates (phi, theta) into Cartesian coordinates relative to the main direction
    private Vector3 SphericalToCartesian(float phi, float theta, Vector3 forward) {
        // Use the spherical coordinate system with the forward direction as the main axis
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(forward, up).normalized;
        up = Vector3.Cross(right, forward).normalized;

        // Cartesian coordinates conversion
        float sinPhi = Mathf.Sin(phi);
        float x = sinPhi * Mathf.Cos(theta);
        float y = sinPhi * Mathf.Sin(theta);
        float z = Mathf.Cos(phi);

        // Transform local direction to world space
        return (x * right + y * up + z * forward).normalized;
    }
}
