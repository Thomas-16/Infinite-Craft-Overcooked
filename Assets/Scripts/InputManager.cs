using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
	public static InputManager Instance { get; private set; }
	public PlayerInputActions inputActions;
	public event Action<int> OnNumberKeyPressed;
	public event Action<int> OnScrollItemSwitch;

    private KeyCode[] numKeyCodes = {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9,
    };

    private void Awake()
	{
		Instance = this;
		inputActions = new PlayerInputActions();
		inputActions.Enable();
	}
    
    private void Update() {
		HandleNumKeyInput();
		HandleScrollItemSwitch();
    }
    //TODO: refactor to use new Input system
    private void HandleNumKeyInput() {
        for (int i = 0; i < numKeyCodes.Length; i++) {
            if (Input.GetKeyDown(numKeyCodes[i])) {
                OnNumberKeyPressed?.Invoke(i + 1);
                return;
            }
        }
    }
    private void HandleScrollItemSwitch() {
        OnScrollItemSwitch?.Invoke((int)-Input.mouseScrollDelta.y);
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

	// Throw charging methods
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

	// Sprint methods
	public bool GetSprintInput()
	{
		return inputActions.Player.Sprint.IsPressed();
	}

	public bool GetSprintPressed()
	{
		return inputActions.Player.Sprint.WasPressedThisFrame();
	}

	public bool GetSprintReleased()
	{
		return inputActions.Player.Sprint.WasReleasedThisFrame();
	}
}