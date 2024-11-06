using UnityEngine;
using System.Collections;

public class LightningSystem : MonoBehaviour
{
	public static LightningSystem Instance { get; private set; }

	[Header("Lightning Effects")]
	[SerializeField] private GameObject lightningBoltPrefab;
	[SerializeField] private GameObject explosionEffectPrefab;
	[SerializeField] private AudioClip lightningSound;

	[Header("Lightning Settings")]
	[SerializeField] private float boltDuration = 0.1f;
	[SerializeField] private float lightningHeight = 50f;
	[SerializeField] private LayerMask hitLayers;

	[Header("Explosion Settings")]
	[SerializeField] private float explosionRadius = 5f;
	[SerializeField] private float explosionForce = 1000f;
	[SerializeField] private float upwardsModifier = 3f;
	[SerializeField] private float destroyDelay = 3f;

	private Camera mainCamera;
	private AudioSource audioSource;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		mainCamera = Camera.main;
		audioSource = gameObject.AddComponent<AudioSource>();
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit, 1000f, hitLayers))
			{
				SpawnLightning(hit.point);
			}
		}
	}

	public void SpawnLightning(Vector3 targetPosition)
	{
		StartCoroutine(LightningSequence(targetPosition));
	}

	private IEnumerator LightningSequence(Vector3 targetPosition)
	{
		// Spawn lightning bolt effect
		Vector3 boltStartPosition = targetPosition + Vector3.up * lightningHeight;
		GameObject lightningBolt = Instantiate(lightningBoltPrefab, boltStartPosition, Quaternion.identity);
		lightningBolt.transform.LookAt(targetPosition);
		lightningBolt.transform.rotation = Quaternion.identity;//Quaternion.LookRotation((targetPosition - boltStartPosition).normalized) *
										 //Quaternion.Euler(90f, 0f, 0f); // Adjust based on your bolt effect's orientation

		// Play lightning sound
		if (lightningSound != null)
		{
			audioSource.PlayOneShot(lightningSound);
		}

		// Wait for bolt duration
		yield return new WaitForSeconds(0.1f);

		// Destroy bolt effect
		Destroy(lightningBolt, destroyDelay);

		// Spawn explosion effect
		GameObject explosion = Instantiate(explosionEffectPrefab, targetPosition, Quaternion.identity);
		Destroy(explosion, destroyDelay);

		// Handle physics and destruction
		ApplyExplosionForce(targetPosition);
		DestroyLLements(targetPosition);
	}

	private void ApplyExplosionForce(Vector3 explosionPoint)
	{
		// Find all colliders in explosion radius
		Collider[] colliders = Physics.OverlapSphere(explosionPoint, explosionRadius);

		foreach (Collider hit in colliders)
		{
			Debug.Log("collider hit: " + hit.gameObject.name);
			Rigidbody rb = hit.GetComponent<Rigidbody>();
			if (rb != null)
			{
				rb.AddExplosionForce(explosionForce, explosionPoint, explosionRadius, upwardsModifier);
			}
		}
	}

	private void DestroyLLements(Vector3 centerPoint)
	{
		// Find all colliders in explosion radius
		Collider[] colliders = Physics.OverlapSphere(centerPoint, explosionRadius/4f);

		foreach (Collider hit in colliders)
		{
			LLement element = hit.GetComponent<LLement>();
			if (element != null)
			{
				Destroy(element.gameObject);
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		// Draw explosion radius in editor
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, explosionRadius);
	}
}