using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
	public static InputManager Instance { get; private set; }
	public PlayerInputActions inputActions;

	private void Awake()
	{
		Instance = this;
		inputActions = new PlayerInputActions();
		inputActions.Enable();
	}

	public Vector2 GetMovementInputVector()
	{
		return inputActions.Player.Movement.ReadValue<Vector2>();
	}

	public bool GetPickupInput()
	{
		return inputActions.Player.Pickup.IsPressed();
	}

	public bool GetInteractInput()
	{
		return inputActions.Player.Interact.IsPressed();
	}

	public bool GetInteractPressed()
	{
		return inputActions.Player.Interact.WasPressedThisFrame();
	}

	public bool GetInteractReleased()
	{
		return inputActions.Player.Interact.WasReleasedThisFrame();
	}

	public float GetCameraZoomInputDelta()
	{
		return Mathf.Clamp(inputActions.Player.CameraZoom.ReadValue<float>(), -110f, 110f);
	}

	// New methods for throw charging
	public bool GetMouseRightButtonDown()
	{
		return inputActions.Player.ThrowCharge.WasPressedThisFrame();
	}

	public bool GetMouseRightButton()
	{
		return inputActions.Player.ThrowCharge.IsPressed();
	}

	public bool GetMouseRightButtonUp()
	{
		return inputActions.Player.ThrowCharge.WasReleasedThisFrame();
	}
}