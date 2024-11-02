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
	[Header("References")]
	[SerializeField] private Transform holdingObjectTransform;
	[SerializeField] private Transform lookingRaycastPositionTransform;

	[Header("Settings")]
	[SerializeField] private bool debugVisualizeRays = true;
	[SerializeField] private float rayCastAngle = 25f;
	[SerializeField] private int numRaycastRays = 20;
	[SerializeField] private float raycastDistance = 1.25f;
	[SerializeField] private float mergeItemsInputHoldThreshold = .75f;

	[Header("Sprint Settings")]
	[SerializeField] private float sprintSpeedMultiplier = 2f;
	[SerializeField] private float maxSprintResource = 100f;
	[SerializeField] private float sprintDrainRate = 25f;    // Units per second
	[SerializeField] private float sprintRecoveryRate = 15f; // Units per second
	[SerializeField] private float sprintRecoveryDelay = 1f; // Seconds to wait before recovery starts
	[SerializeField] private UIPanel sprintBarPanelPrefab;
	[SerializeField] private Transform sprintBarTransformReference;

	[Header("Nametag Settings")]
	[SerializeField] private string playerName = "Player";
	[SerializeField] private float nametagOffset = 2.5f;
	[SerializeField] private Color nametagColor = Color.white;
	[SerializeField] private UIPanel nametagPrefab;

	[Header("Throwing Settings")]
	[SerializeField] private float minThrowForce = 500f;
	[SerializeField] private float maxThrowForce = 2000f;
	[SerializeField] private float maxChargeTime = 1.5f;
	[SerializeField] private float throwUpwardAngle = 0.1f;
	[SerializeField] private float rotationSpeed = 720f;
	[SerializeField] private LayerMask groundLayer;
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

	protected Character _character;
	private Camera mainCamera;
	private ConeCastHelper coneCastHelper;

	private UIPanel nametagPanel;

	private float defaultWalkSpeed;
	private bool isSprinting;
	private SprintBar sprintBar;
	private UIPanel sprintBarPanel;

	[SerializeField] private float currentSprintResource;
	private float sprintRecoveryTimer;
	private bool canSprint => currentSprintResource > 0f;

	protected virtual void Awake()
	{
		_character = GetComponent<Character>();
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
		InputManager.Instance.inputActions.Player.Sprint.started += OnSprintPressed;
		InputManager.Instance.inputActions.Player.Sprint.canceled += OnSprintReleased;

		SetupNametag();

		defaultWalkSpeed = _character.maxWalkSpeed;
		currentSprintResource = maxSprintResource;
		SetupSprintBar();
	}

	private void SetupSprintBar()
	{
		if (UIManager.Instance != null)
		{
			sprintBarPanel = UIManager.Instance.CreateWorldPositionedPanel(
				sprintBarTransformReference,
				sprintBarPanelPrefab,
				Vector3.zero
			);

			sprintBar = sprintBarPanel.GetComponent<SprintBar>();

			if (sprintBarPanel != null)
			{
				UpdateSprintBarUI();
			}
		}
	}

	private void HandleSprintResource()
	{
		if (isSprinting && canSprint)
		{
			// Drain sprint resource
			currentSprintResource = Mathf.Max(0f, currentSprintResource - (sprintDrainRate * Time.deltaTime));
			sprintRecoveryTimer = 0f;

			// Force stop sprinting if resource is depleted
			if (!canSprint)
			{
				StopSprinting();
			}
		}
		else
		{
			// Handle recovery timer
			if (sprintRecoveryTimer < sprintRecoveryDelay)
			{
				sprintRecoveryTimer += Time.deltaTime;
			}
			else if (currentSprintResource < maxSprintResource)
			{
				// Recover sprint resource
				currentSprintResource = Mathf.Min(maxSprintResource,
					currentSprintResource + (sprintRecoveryRate * Time.deltaTime));
			}
		}

		UpdateSprintBarUI();
	}

	private void UpdateSprintBarUI()
	{
		if (sprintBar != null)
		{
			float sprintPercentage = currentSprintResource / maxSprintResource;
			bool isRecovering = !isSprinting && sprintPercentage < 1f;
			sprintBar.UpdateSprintBar(sprintPercentage, isSprinting, isRecovering);
		}
	}

	private void OnSprintPressed(InputAction.CallbackContext context)
	{
		if (_character.IsWalking() && !_character.IsCrouched() && canSprint)
		{
			isSprinting = true;
			_character.maxWalkSpeed = defaultWalkSpeed * sprintSpeedMultiplier;
		}
	}

	private void OnSprintReleased(InputAction.CallbackContext context)
	{
		StopSprinting();
	}

	private void StopSprinting()
	{
		if (isSprinting)
		{
			isSprinting = false;
			_character.maxWalkSpeed = defaultWalkSpeed;
		}
	}

	private void SetupNametag()
	{
		if (UIManager.Instance != null)
		{
			nametagPanel = UIManager.Instance.CreateWorldPositionedPanel(
				transform,
				nametagPrefab,  // You'll need to add this prefab reference to UIManager
				new Vector3(0, nametagOffset, 0)
			);

			if (nametagPanel != null)
			{
				// Set initial nametag text and color
				nametagPanel.SetText(playerName);
				nametagPanel.SetTextColor(Color.white);
				nametagPanel.SetPanelColor(nametagColor);
			}
		}
	}

	private void OnDestroy()
	{
		if (UIManager.Instance != null)
		{
			if (nametagPanel != null)
				UIManager.Instance.RemoveWorldPositionedPanel(transform);
			if (sprintBarPanel != null)
				UIManager.Instance.RemoveWorldPositionedPanel(transform);
		}
	}


	// Getter/setter for player name that updates the UI
	public string PlayerName
	{
		get => playerName;
		set
		{
			playerName = value;
			if (nametagPanel != null)
			{
				nametagPanel.SetText(value);
			}
		}
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
		HandleInteractInput();
		HandleSprintResource();
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

			// Apply movement modifiers
			if (isChargingThrow)
			{
				movementDirection *= moveSpeedMultiplierWhileThrowing;
			}

			if (isSprinting && movementDirection.magnitude > 0.1f && !isChargingThrow)
			{
				movementDirection *= sprintSpeedMultiplier;
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