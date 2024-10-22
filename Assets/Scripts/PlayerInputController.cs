using ECM2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{

    protected Character _character;

    //public virtual void AddControlZoomInput(float value) {
    //    followDistance = Mathf.Clamp(followDistance - value, followMinDistance, followMaxDistance);
    //}

    protected virtual void Awake() {
        _character = GetComponent<Character>();
    }

    protected virtual void Start() {
        Cursor.lockState = CursorLockMode.Locked;

        InputManager.Instance.inputActions.Player.Crouch.started += OnCrouchPressed;
        InputManager.Instance.inputActions.Player.Crouch.canceled += OnCrouchReleased;
        InputManager.Instance.inputActions.Player.Jump.started += OnJumpPressed;
        InputManager.Instance.inputActions.Player.Jump.canceled += OnJumpReleased;
    }

    protected virtual void Update() {
        // Movement input

        Vector2 inputMove = InputManager.Instance.GetMovementInputVector();

        Vector3 movementDirection = Vector3.zero;

        movementDirection += Vector3.right * inputMove.x;
        movementDirection += Vector3.forward * inputMove.y;

        if (_character.cameraTransform)
            movementDirection = movementDirection.relativeTo(_character.cameraTransform, _character.GetUpVector());

        _character.SetMovementDirection(movementDirection);

    }
    private void OnCrouchPressed(InputAction.CallbackContext context) {
        _character.Crouch();
    }
    private void OnCrouchReleased(InputAction.CallbackContext context) {
        _character.UnCrouch();
    }
    private void OnJumpPressed(InputAction.CallbackContext context) {
        _character.Jump();
    }
    private void OnJumpReleased(InputAction.CallbackContext context) {
        _character.StopJumping();
    }

}
