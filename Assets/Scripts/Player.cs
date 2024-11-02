using Cinemachine;
using ECM2;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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
	[SerializeField] private ParticleSystem chargingParticleSystem;
	[SerializeField] private AnimationCurve chargingParticlesCurve = new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 2f));
	[SerializeField] private Transform powerBarTransformReference;

	[Header("Throw UI Settings")]
	[SerializeField] private UIPanel throwPowerBarPrefab;
	[SerializeField] private float throwBarOffset = 1.75f;

	private PickupableObject hoveringObject;
	private PickupableObject pickedupObject;
	private bool isHoldingObject;
	private float pickupInputStartTime;
	private bool pickupInputActive = false;
	private bool justPickedUp = false;

	private float throwChargeStartTime;
	private bool isChargingThrow = false;
	private ParticleSystem.EmissionModule emissionModule;
	private UIPanel throwPowerBarPanel;
	private ThrowPowerBar throwPowerBar;

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
		SetupSprintBar();
		SetupThrowUI();

		defaultWalkSpeed = _character.maxWalkSpeed;
		currentSprintResource = maxSprintResource;
	}

	private void SetupThrowUI()
	{
		if (UIManager.Instance != null)
		{
			throwPowerBarPanel = UIManager.Instance.CreateWorldPositionedPanel(
				powerBarTransformReference,
				throwPowerBarPrefab,
				Vector3.zero
			);

			throwPowerBar = throwPowerBarPanel.GetComponent<ThrowPowerBar>();
		}
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

	private void SetupNametag()
	{
		if (UIManager.Instance != null)
		{
			nametagPanel = UIManager.Instance.CreateWorldPositionedPanel(
				transform,
				nametagPrefab,
				new Vector3(0, nametagOffset, 0)
			);

			if (nametagPanel != null)
			{
				nametagPanel.SetText(playerName);
				nametagPanel.SetTextColor(Color.white);
				nametagPanel.SetPanelColor(nametagColor);
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

	private void HandleSprintResource()
	{
		if (isSprinting && canSprint)
		{
			currentSprintResource = Mathf.Max(0f, currentSprintResource - (sprintDrainRate * Time.deltaTime));
			sprintRecoveryTimer = 0f;

			if (!canSprint)
			{
				StopSprinting();
			}
		}
		else
		{
			if (sprintRecoveryTimer < sprintRecoveryDelay)
			{
				sprintRecoveryTimer += Time.deltaTime;
			}
			else if (currentSprintResource < maxSprintResource)
			{
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
		if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
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

		if (throwPowerBar != null)
		{
			throwPowerBar.UpdatePowerBar(chargePercent, isChargingThrow);
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
		if (throwPowerBar != null)
		{
			throwPowerBar.UpdatePowerBar(0f, false);
		}

		if (chargingParticleSystem != null)
		{
			chargingParticleSystem.Stop();
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
					isHoldingObject = false;
					pickedupObject.Drop(this);
					pickedupObject = null;
				}
				else if (hoveringObject != null)
				{
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

	public Transform GetHoldingObjectSpotTransform()
	{
		return holdingObjectTransform;
	}

	private void OnDestroy()
	{
		if (UIManager.Instance != null)
		{
			if (nametagPanel != null)
				UIManager.Instance.RemoveWorldPositionedPanel(transform);
			if (sprintBarPanel != null)
				UIManager.Instance.RemoveWorldPositionedPanel(transform);
			if (throwPowerBarPanel != null)
				UIManager.Instance.RemoveWorldPositionedPanel(transform);
		}
	}
}