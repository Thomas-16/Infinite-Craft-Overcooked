using Cinemachine;
using ECM2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
	[Header("Camera Zoom Control")]
	private float cameraZoom = 7.6f;
	[SerializeField] private float zoomSpeed = 10f;
	private float minZoom = 3.95f;
	private float maxZoom = 9.77f;

	private Queue<float> zoomInputs = new Queue<float>();
	private int bufferSize = 5;

	protected Character _character;
	private CinemachineVirtualCamera virtualCamera;
	private ConeCastHelper coneCastHelper;
	private Camera mainCamera;

	[Header("References")]
	[SerializeField] private Transform holdingObjectTransform;
	[SerializeField] private Transform lookingRaycastPositionTransform;

	[Header("Settings")]
	[SerializeField] private bool debugVisualizeRays = true;
	[SerializeField] private float rayCastAngle = 25f;
	[SerializeField] private int numRaycastRays = 20;
	[SerializeField] private float raycastDistance = 1.25f;
	[SerializeField] private float mergeItemsInputHoldThreshold = .75f;

	[Header("Throwing Settings")]
	[SerializeField] private float minThrowForce = 500f;
	[SerializeField] private float maxThrowForce = 2000f;
	[SerializeField] private float maxChargeTime = 1.5f;
	[SerializeField] private float throwUpwardAngle = 0.1f;
	[SerializeField] private float rotationSpeed = 720f;
	[SerializeField] private LayerMask groundLayer;

	[Header("Movement Settings")]
	[SerializeField] private float moveSpeedMultiplierWhileThrowing = 0.8f;

	[Header("Throw Visual Feedback")]
	[SerializeField] private GameObject throwPowerUIPanel;
	[SerializeField] private Image throwPowerFillBar;
	[SerializeField] private ParticleSystem chargingParticleSystem;
	[SerializeField] private Color minChargeColor = Color.yellow;
	[SerializeField] private Color maxChargeColor = Color.red;
	[SerializeField] private AnimationCurve chargingParticlesCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 2f);

	private PickupableObject hoveringObject;
	private PickupableObject pickedupObject;
	private bool isHoldingObject;
	private float pickupInputStartTime;
	private bool pickupInputActive = false;
	private bool justPickedUp = false;

	private float throwChargeStartTime;
	private bool isChargingThrow = false;
	private ParticleSystem.EmissionModule emissionModule;

	private float lastTriedToMergeTime;
	private bool interactInputActive;

	protected virtual void Awake()
	{
		_character = GetComponent<Character>();
		virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
		mainCamera = Camera.main;

		coneCastHelper = new ConeCastHelper();
		coneCastHelper.InitializeConeCast(rayCastAngle, numRaycastRays);

		if (chargingParticleSystem != null)
		{
			emissionModule = chargingParticleSystem.emission;
			chargingParticleSystem.Stop();
		}

		if (throwPowerUIPanel != null)
		{
			throwPowerUIPanel.SetActive(false);
		}
	}

	protected virtual void Start()
	{
		InputManager.Instance.inputActions.Player.Crouch.started += OnCrouchPressed;
		InputManager.Instance.inputActions.Player.Crouch.canceled += OnCrouchReleased;
		InputManager.Instance.inputActions.Player.Jump.started += OnJumpPressed;
		InputManager.Instance.inputActions.Player.Jump.canceled += OnJumpReleased;
	}

	protected virtual void Update()
	{
		if (isChargingThrow)
		{
			UpdateRotationTowardsMouse();
		}

		HandleMovement();
		HandleHoverObjects();
		HandlePickupInput();
		HandleThrowInput();
		HandleCameraZoom();
		HandleInteractInput();
	}

	private void HandleMovement()
	{
		Vector2 inputMove = InputManager.Instance.GetMovementInputVector();
		Vector3 movementDirection = Vector3.zero;

		movementDirection += Vector3.right * inputMove.x;
		movementDirection += Vector3.forward * inputMove.y;

		if (_character.cameraTransform)
		{
			movementDirection = movementDirection.relativeTo(_character.cameraTransform, _character.GetUpVector());

			if (isChargingThrow)
			{
				movementDirection *= moveSpeedMultiplierWhileThrowing;
			}
		}

		_character.SetMovementDirection(movementDirection);
	}

	private void UpdateRotationTowardsMouse()
	{
		Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit, 100f, groundLayer))
		{
			Vector3 targetDirection = hit.point - transform.position;
			targetDirection.y = 0;

			if (targetDirection != Vector3.zero)
			{
				Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
				transform.rotation = Quaternion.RotateTowards(
					transform.rotation,
					targetRotation,
					rotationSpeed * Time.deltaTime
				);
			}
		}
	}

	private void HandleThrowInput()
	{
		if (isHoldingObject)
		{
			if (InputManager.Instance.GetMouseRightButtonDown() && !isChargingThrow)
			{
				StartThrowCharge();
			}
			else if (InputManager.Instance.GetMouseRightButton() && isChargingThrow)
			{
				UpdateThrowCharge();
			}
			else if (InputManager.Instance.GetMouseRightButtonUp() && isChargingThrow)
			{
				ReleaseThrow();
			}
		}
	}

	private void StartThrowCharge()
	{
		isChargingThrow = true;
		throwChargeStartTime = Time.time;

		if (throwPowerUIPanel != null)
		{
			throwPowerUIPanel.SetActive(true);
			throwPowerFillBar.fillAmount = 0f;
		}

		if (chargingParticleSystem != null)
		{
			chargingParticleSystem.transform.position = pickedupObject.transform.position;
			chargingParticleSystem.Play();
		}
	}

	private void UpdateThrowCharge()
	{
		float chargeTime = Mathf.Min(Time.time - throwChargeStartTime, maxChargeTime);
		float chargePercent = chargeTime / maxChargeTime;

		if (throwPowerFillBar != null)
		{
			throwPowerFillBar.fillAmount = chargePercent;
			throwPowerFillBar.color = Color.Lerp(minChargeColor, maxChargeColor, chargePercent);
		}

		if (chargingParticleSystem != null)
		{
			chargingParticleSystem.transform.position = pickedupObject.transform.position;
			float emissionRate = chargingParticlesCurve.Evaluate(chargePercent);
			emissionModule.rateOverTime = emissionRate * 50f;
		}
	}

	private void ReleaseThrow()
	{
		float chargeTime = Mathf.Min(Time.time - throwChargeStartTime, maxChargeTime);
		float chargePercent = chargeTime / maxChargeTime;
		float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargePercent);

		Vector3 throwDirection = transform.forward + (Vector3.up * throwUpwardAngle);
		throwDirection.Normalize();

		isHoldingObject = false;
		pickedupObject.Drop(this);
		pickedupObject.GetComponent<Rigidbody>().AddForce(throwDirection * throwForce);
		pickedupObject = null;

		isChargingThrow = false;
		if (throwPowerUIPanel != null)
		{
			throwPowerUIPanel.SetActive(false);
		}
		if (chargingParticleSystem != null)
		{
			chargingParticleSystem.Stop();
		}
	}

	private void HandlePickupInput()
	{
		if (InputManager.Instance.GetPickupInput())
		{
			if (!pickupInputActive)
			{
				pickupInputActive = true;
				pickupInputStartTime = Time.time;

				if (isHoldingObject)
				{
					// Drop currently held object
					isHoldingObject = false;
					pickedupObject.Drop(this);
					pickedupObject = null;
				}
				else if (hoveringObject != null)
				{
					// Pick up new object
					isHoldingObject = true;
					hoveringObject.GetComponent<Rigidbody>().isKinematic = true;
					hoveringObject.Pickup(this);
					pickedupObject = hoveringObject;
					justPickedUp = true;
				}
			}
		}
		else if (pickupInputActive)
		{
			pickupInputActive = false;
			justPickedUp = false;
		}
	}

	private void HandleHoverObjects()
	{
		RaycastHit[] raycastHits = coneCastHelper.ConeCast(lookingRaycastPositionTransform.position, transform.forward, raycastDistance);
		if (debugVisualizeRays)
		{
			foreach (var hit in raycastHits)
			{
				Debug.DrawLine(lookingRaycastPositionTransform.position, hit.point, Color.red);
			}
		}

		foreach (RaycastHit hit in raycastHits)
		{
			PickupableObject pickupableObject = hit.collider.GetComponentInParent<PickupableObject>();
			if (pickupableObject != null && !pickupableObject.IsPickedUp)
			{
				pickupableObject.HoverOver(this);
				hoveringObject = pickupableObject;
				return;
			}
		}
		hoveringObject = null;
	}

	private void HandleInteractInput()
	{
		if (InputManager.Instance.GetInteractPressed())
		{
			interactInputActive = true;
		}
		if (InputManager.Instance.GetInteractReleased())
		{
			interactInputActive = false;
		}
	}

	private void HandleCameraZoom()
	{
		float zoomInput = InputManager.Instance.GetCameraZoomInputDelta();

		zoomInputs.Enqueue(zoomInput);
		if (zoomInputs.Count > bufferSize)
		{
			zoomInputs.Dequeue();
		}

		float averageZoomInput = GetAverageZoomInput();

		if (Mathf.Abs(averageZoomInput) > 0.01f)
		{
			float targetZoom = Mathf.Clamp(cameraZoom - averageZoomInput, minZoom, maxZoom);
			cameraZoom = Mathf.Lerp(cameraZoom, targetZoom, zoomSpeed * Time.deltaTime);

			virtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset =
				new Vector3(0, cameraZoom, CameraZoomZFunction(cameraZoom));
			virtualCamera.transform.rotation = Quaternion.Euler(CameraRotationXFunction(cameraZoom), 0f, 0f);
		}
	}

	private float GetAverageZoomInput()
	{
		float sum = 0f;
		foreach (float input in zoomInputs)
		{
			sum += input;
		}
		return sum / zoomInputs.Count;
	}

	private float CameraZoomZFunction(float y)
	{
		return (0.1375f * y * y) - (2.149f * y) + 4.196f;
	}

	private float CameraRotationXFunction(float y)
	{
		return (0.6286f * y * y) - (7.124f * y) + 78.95f;
	}

	public Transform GetHoldingObjectSpotTransform()
	{
		return holdingObjectTransform;
	}

	private void OnCrouchPressed(InputAction.CallbackContext context)
	{
		_character.Crouch();
	}

	private void OnCrouchReleased(InputAction.CallbackContext context)
	{
		_character.UnCrouch();
	}

	private void OnJumpPressed(InputAction.CallbackContext context)
	{
		_character.Jump();
	}

	private void OnJumpReleased(InputAction.CallbackContext context)
	{
		_character.StopJumping();
	}
}