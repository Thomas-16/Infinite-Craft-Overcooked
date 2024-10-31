using UnityEngine;

public class GameBillboard : MonoBehaviour
{
	private Camera mainCamera;

	[Header("Billboard Settings")]
	[SerializeField] private bool freezeXZAxis = true;
	[SerializeField] private Vector3 rotationOffset = new Vector3(0, 180, 0);

	private void Start()
	{
		mainCamera = Camera.main;
	}

	private void LateUpdate()
	{
		if (mainCamera == null) return;

		if (freezeXZAxis)
		{
			// For orthographic camera, we can just match the camera's Y rotation
			// This keeps the sprite upright while rotating to face camera
			Vector3 eulerAngles = mainCamera.transform.rotation.eulerAngles;
			transform.rotation = Quaternion.Euler(0, eulerAngles.y, 0) * Quaternion.Euler(rotationOffset);
		}
		else
		{
			// Full billboarding - just match camera rotation
			transform.rotation = mainCamera.transform.rotation * Quaternion.Euler(rotationOffset);
		}

		// Ensure sprite is perfectly flat relative to camera view
		// This prevents any perspective skewing
		if (mainCamera.orthographic)
		{
			// Align the sprite to be parallel to the camera's near plane
			Vector3 camForward = mainCamera.transform.forward;
			transform.forward = -camForward;
		}
	}

	// Optional: Debug visualization
	private void OnDrawGizmos()
	{
		if (mainCamera != null && Debug.isDebugBuild)
		{
			// Draw sprite forward direction
			Gizmos.color = Color.blue;
			Gizmos.DrawRay(transform.position, transform.forward);

			// Draw camera direction
			Gizmos.color = Color.red;
			Gizmos.DrawRay(transform.position, -mainCamera.transform.forward);
		}
	}
}