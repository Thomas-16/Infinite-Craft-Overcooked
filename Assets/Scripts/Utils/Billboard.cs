using UnityEngine;

public class Billboard : MonoBehaviour
{
	[SerializeField] private Camera mainCamera;

	[Tooltip("If true, only rotates around Y axis to face camera")]
	[SerializeField] private bool lockVertical = false;

	private void Start()
	{
		mainCamera = Camera.main;
	}

	private void LateUpdate()
	{
		if (mainCamera == null)
			return;

		if (lockVertical)
		{
			// Only rotate around Y axis
			Vector3 directionToCamera = mainCamera.transform.position - transform.position;
			directionToCamera.y = 0; // Remove vertical component

			if (directionToCamera != Vector3.zero)
			{
				transform.rotation = Quaternion.LookRotation(-directionToCamera);
			}
		}
		else
		{
			// Full rotation to face camera
			transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
						   mainCamera.transform.rotation * Vector3.up);
		}
	}
}