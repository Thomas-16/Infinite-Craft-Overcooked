using UnityEngine;

public class RandomSpherePlacement : MonoBehaviour
{
    public GameObject[] prefabs; // Array of prefabs to place
    public int numPrefabs = 10; // Number of prefabs to place
    public float sphereRadius = 10f; // Radius of the sphere
    public Transform sphereCenter; // Center of the sphere

    void Start() {
        PlacePrefabsOnSphere();
    }

    private void PlacePrefabsOnSphere() {
        for (int i = 0; i < numPrefabs; i++) {
            // Choose a random prefab from the array
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];

            // Generate a random point on the surface of the sphere
            Vector3 randomDirection = Random.onUnitSphere; // Random direction on the unit sphere
            Vector3 position = sphereCenter.position + randomDirection * sphereRadius;

            // Instantiate the prefab at the position
            GameObject instance = Instantiate(prefab, position, Quaternion.identity, sphereCenter);

            // Orient the prefab to face outward from the sphere's center
            instance.transform.up = randomDirection;

            // Apply a random rotation around the "up" axis
            float randomRotation = Random.Range(0f, 360f);
            instance.transform.Rotate(Vector3.up, randomRotation, Space.Self);
        }
    }
}
