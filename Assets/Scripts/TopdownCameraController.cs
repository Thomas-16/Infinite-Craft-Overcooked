using ECM2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopdownCameraController : MonoBehaviour
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
    }

    protected virtual void Update() {
        // Movement input

        Vector2 inputMove = new Vector2() {
            x = Input.GetAxisRaw("Horizontal"),
            y = Input.GetAxisRaw("Vertical")
        };

        Vector3 movementDirection = Vector3.zero;

        movementDirection += Vector3.right * inputMove.x;
        movementDirection += Vector3.forward * inputMove.y;

        if (_character.cameraTransform)
            movementDirection = movementDirection.relativeTo(_character.cameraTransform, _character.GetUpVector());

        _character.SetMovementDirection(movementDirection);

        // Crouch input

        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            _character.Crouch();
        else if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.C))
            _character.UnCrouch();

        // Jump input

        if (Input.GetButtonDown("Jump"))
            _character.Jump();
        else if (Input.GetButtonUp("Jump"))
            _character.StopJumping();

    }
}
