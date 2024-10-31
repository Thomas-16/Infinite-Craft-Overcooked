using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class OrbitCameraController : MonoBehaviour
{
	[Header("Camera References")]
	[SerializeField] private CinemachineVirtualCamera virtualCamera;
	[SerializeField] private Transform cameraTarget;

	[Header("Follow Settings")]
	[SerializeField] private float dampingSpeed = 5f;
	[SerializeField] private float baseHeightOffset = 10f;
	[SerializeField] private float baseDistanceOffset = 10f;

	[Header("Rotation Settings")]
	[SerializeField] private float rotationAmount = 45f;
	[SerializeField] private float rotationDuration = 0.3f;
	[SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

	[Header("Zoom Settings")]
	[SerializeField] private float minZoom = 5f;
	[SerializeField] private float maxZoom = 20f;
	[SerializeField] private float zoomSpeed = 2f;
	[SerializeField] private float basePitchAngle = 45f;
	[SerializeField] private float minPitchAngle = 30f;  // More top-down when zoomed in
	[SerializeField] private float maxPitchAngle = 60f;  // More angled when zoomed out
	[SerializeField] private AnimationCurve heightMultiplierCurve = AnimationCurve.Linear(0, 0.5f, 1, 1.5f);
	[SerializeField] private AnimationCurve distanceMultiplierCurve = AnimationCurve.Linear(0, 0.5f, 1, 1.5f);

	private float currentRotation = 0f;
	private float targetRotation = 0f;
	private float rotationTimer = 0f;
	private float startRotation = 0f;
	private bool isRotating = false;

	private float currentZoom;
	private float targetZoom;

	private void Start()
	{
		if (virtualCamera == null)
		{
			virtualCamera = GetComponent<CinemachineVirtualCamera>();
		}

		// Setup orthographic camera
		var brain = Camera.main.GetComponent<CinemachineBrain>();
		if (brain == null)
		{
			Camera.main.gameObject.AddComponent<CinemachineBrain>();
		}

		// Configure virtual camera
		virtualCamera.m_Lens.Orthographic = true;

		// Initialize zoom
		currentZoom = targetZoom = (minZoom + maxZoom) * 0.5f;
		virtualCamera.m_Lens.OrthographicSize = currentZoom;

		// Setup input bindings
		InputManager.Instance.inputActions.Player.RotateLeft.performed += OnRotateLeft;
		InputManager.Instance.inputActions.Player.RotateRight.performed += OnRotateRight;
	}

	private void Update()
	{
		HandleZoom();
		HandleRotation();
		UpdateCameraTransform();
	}

	private void HandleZoom()
	{
		float zoomInput = InputManager.Instance.GetCameraZoomInputDelta();
		if (Mathf.Abs(zoomInput) > 0.01f)
		{
			// Invert zoom input for more intuitive scrolling
			targetZoom = Mathf.Clamp(targetZoom - zoomInput * zoomSpeed, minZoom, maxZoom);
		}

		// Smooth zoom transition
		currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * dampingSpeed);
		virtualCamera.m_Lens.OrthographicSize = currentZoom;
	}

	private void HandleRotation()
	{
		if (isRotating)
		{
			rotationTimer += Time.deltaTime;
			float normalizedTime = rotationTimer / rotationDuration;

			if (normalizedTime >= 1f)
			{
				currentRotation = targetRotation;
				isRotating = false;
			}
			else
			{
				float curveValue = rotationCurve.Evaluate(normalizedTime);
				currentRotation = Mathf.Lerp(startRotation, targetRotation, curveValue);
			}
		}
	}

	private void UpdateCameraTransform()
	{
		if (cameraTarget == null) return;

		// Calculate zoom progress (0 = most zoomed in, 1 = most zoomed out)
		float zoomProgress = Mathf.InverseLerp(minZoom, maxZoom, currentZoom);

		// Calculate dynamic pitch angle based on zoom
		float currentPitchAngle = Mathf.Lerp(minPitchAngle, maxPitchAngle, zoomProgress);

		// Calculate offset multipliers based on zoom
		float heightMultiplier = heightMultiplierCurve.Evaluate(zoomProgress);
		float distanceMultiplier = distanceMultiplierCurve.Evaluate(zoomProgress);

		// Calculate the current height and distance offsets
		float currentHeight = baseHeightOffset * heightMultiplier;
		float currentDistance = baseDistanceOffset * distanceMultiplier;

		Vector3 targetPos = cameraTarget.position;
		Vector3 forward = Quaternion.Euler(0, currentRotation, 0) * Vector3.forward;

		// Calculate position using dynamic offsets
		Vector3 heightOffsetVector = Vector3.up * currentHeight;
		Vector3 distanceOffsetVector = -forward * currentDistance;
		Vector3 desiredPosition = targetPos + heightOffsetVector + distanceOffsetVector;

		// Create rotation for the camera's dynamic pitch and yaw
		Quaternion pitchRotation = Quaternion.Euler(currentPitchAngle, currentRotation, 0);

		// Apply dampening to position
		transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * dampingSpeed);
		transform.rotation = pitchRotation;
	}

	private void OnRotateLeft(InputAction.CallbackContext context)
	{
		RotateCamera(-rotationAmount);
	}

	private void OnRotateRight(InputAction.CallbackContext context)
	{
		RotateCamera(rotationAmount);
	}

	private void RotateCamera(float amount)
	{
		if (isRotating) return;

		isRotating = true;
		rotationTimer = 0f;
		startRotation = currentRotation;
		targetRotation = currentRotation + amount;
	}

	public void SetTarget(Transform newTarget)
	{
		cameraTarget = newTarget;
	}

	private void OnDestroy()
	{
		if (InputManager.Instance != null)
		{
			InputManager.Instance.inputActions.Player.RotateLeft.performed -= OnRotateLeft;
			InputManager.Instance.inputActions.Player.RotateRight.performed -= OnRotateRight;
		}
	}

	private void OnDrawGizmos()
	{
		if (cameraTarget == null) return;

		Gizmos.color = Color.yellow;
		Vector3 targetPos = cameraTarget.position;

		// Calculate current offsets for gizmos
		float zoomProgress = Mathf.InverseLerp(minZoom, maxZoom, currentZoom);
		float heightMultiplier = heightMultiplierCurve.Evaluate(zoomProgress);
		float distanceMultiplier = distanceMultiplierCurve.Evaluate(zoomProgress);

		Vector3 heightOffset = Vector3.up * (baseHeightOffset * heightMultiplier);
		Vector3 forward = Quaternion.Euler(0, currentRotation, 0) * Vector3.forward;
		Vector3 distanceOffset = -forward * (baseDistanceOffset * distanceMultiplier);

		// Draw offset visualization
		Gizmos.DrawLine(targetPos, targetPos + heightOffset);
		Gizmos.DrawLine(targetPos + heightOffset, targetPos + heightOffset + distanceOffset);
	}
}