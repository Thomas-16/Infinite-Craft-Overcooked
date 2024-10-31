using UnityEngine;

public class GameBillboard : MonoBehaviour
{
	private Camera mainCamera;

	[Header("Billboard Settings")]
	[SerializeField] private bool freezeXZAxis = true;
	[SerializeField] private Vector3 rotationOffset = new Vector3(0, 0, 0); // Changed default offset

	private void Start()
	{
		mainCamera = Camera.main;
	}

	private void LateUpdate()
	{
		if (mainCamera == null) return;

		if (freezeXZAxis)
		{
			// For orthographic camera, match the camera's Y rotation but inverted
			Vector3 eulerAngles = mainCamera.transform.rotation.eulerAngles;
			transform.rotation = Quaternion.Euler(0, eulerAngles.y + 180f, 0) * Quaternion.Euler(rotationOffset);
		}
		else
		{
			// Full billboarding - match camera rotation but face towards camera
			transform.rotation = Quaternion.LookRotation(
				transform.position - mainCamera.transform.position,
				mainCamera.transform.up
			) * Quaternion.Euler(rotationOffset);
		}

		// Ensure sprite faces camera in orthographic mode
		if (mainCamera.orthographic)
		{
			Vector3 camForward = mainCamera.transform.forward;
			transform.forward = camForward; // Changed from -camForward to camForward
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
			Gizmos.DrawRay(transform.position, mainCamera.transform.forward);
		}
	}
}