using UnityEngine;

public class GameBillboard : MonoBehaviour
{
	private Camera mainCamera;

	[Header("Billboard Settings")]
	[SerializeField] private bool freezeXZAxis = true; // Keeps the object vertical
	[SerializeField] private Vector3 rotationOffset = new Vector3(0, 180, 0); // Adjust facing direction if needed

	private void Start()
	{
		mainCamera = Camera.main;
	}

	private void LateUpdate()
	{
		if (mainCamera == null) return;

		if (freezeXZAxis)
		{
			// Keep the object vertical while facing camera
			Vector3 directionToCamera = mainCamera.transform.position - transform.position;
			directionToCamera.y = 0; // Remove vertical component

			if (directionToCamera != Vector3.zero)
			{
				transform.rotation = Quaternion.LookRotation(directionToCamera) * Quaternion.Euler(rotationOffset);
			}
		}
		else
		{
			// Full billboarding
			transform.rotation = mainCamera.transform.rotation * Quaternion.Euler(rotationOffset);
		}
	}
}