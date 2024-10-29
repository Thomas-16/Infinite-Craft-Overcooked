using Cinemachine;
using ECM2;
using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Camera Zoom Control")]
    private float cameraZoom = 7.6f;
    [SerializeField] private float zoomSpeed = 10f;
    private float minZoom = 3.95f;
    private float maxZoom = 9.77f;

    //private Queue<float> zoomInputs = new Queue<float>();  // Queue to store the last 5 zoom inputs
    //private int bufferSize = 5;  // Number of frames to average over

    protected Character _character;
    //private CinemachineVirtualCamera virtualCamera;
    private ConeCastHelper coneCastHelper;

    [Header("References")]
    [SerializeField] private Transform holdingObjectTransform;
    [SerializeField] private Transform lookingRaycastPositionTransform;

    [Header("Settings")]
    [SerializeField] private bool debugVisualizeRays = true;
    [SerializeField] private float rayCastAngle = 25f;
    [SerializeField] private int numRaycastRays = 20;
    [SerializeField] private float raycastDistance = 1.25f;
    [SerializeField] private float throwItemInputHoldThreshold = 0.4f;
    [SerializeField] private float mergeItemsInputHoldThreshold = .75f;

    private PickupableObject hoveringObject;
    private PickupableObject pickedupObject;
    private bool isHoldingObject;
    private float pickupInputStartTime;
    private bool pickupInputActive = false;
    private bool justPickedUp = false;
    private float timeLastThrew;

    private float lastTriedToMergeTime;
    private bool interactInputActive;

    protected virtual void Awake() {
        _character = GetComponent<Character>();
        //virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        coneCastHelper = new ConeCastHelper();
        coneCastHelper.InitializeConeCast(rayCastAngle, numRaycastRays);
    }

    protected virtual void Start() {
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

        HandleHoverObjects();
        HandlePickupInput();
        //HandleCameraZoom();
        HandleInteractInput();
    }

    private void HandleInteractInput() {
        if (InputManager.Instance.GetInteractPressed()) {
            interactInputActive = true;
            if(isHoldingObject && hoveringObject != null && pickedupObject is LLement && hoveringObject is LLement) {
                lastTriedToMergeTime = Time.time;
            }
        }
        if (interactInputActive) {
            if (Time.time - lastTriedToMergeTime > mergeItemsInputHoldThreshold && isHoldingObject && hoveringObject != null && pickedupObject is LLement && hoveringObject is LLement) {
                // merge items
                interactInputActive = false;

                GameManager.Instance.MergeElements(hoveringObject as LLement, pickedupObject as LLement);

                isHoldingObject = false;
                hoveringObject = null;
                pickedupObject = null;
            }
        }
        if(InputManager.Instance.GetInteractReleased()) {
            interactInputActive = false;
        }
    }
    private void HandlePickupInput() {
        if (InputManager.Instance.GetPickupInput()) {
            if (!pickupInputActive) {
                // Pickup input has started
                pickupInputActive = true;
                pickupInputStartTime = Time.time;

                // Pick up the object instantly if hovering over an object and not already holding one
                if (hoveringObject != null && !isHoldingObject && Time.time - timeLastThrew > 0.5f) {
                    isHoldingObject = true;
                    hoveringObject.GetComponent<Rigidbody>().isKinematic = true;
                    hoveringObject.Pickup(this);
                    pickedupObject = hoveringObject;
                    justPickedUp = true;  // Set flag to prevent immediate drop
                }
            }
            else if (isHoldingObject && !justPickedUp && Time.time - pickupInputStartTime >= throwItemInputHoldThreshold) {
                // Throw the object immediately when the input is held for more than 0.5 seconds
                //Debug.Log("throwing");
                isHoldingObject = false;
                pickedupObject.Drop(this);
                pickedupObject.GetComponent<Rigidbody>().AddExplosionForce(1500f, holdingObjectTransform.position - (transform.forward * 0.2f), 0.5f, 0.1f);
                pickedupObject = null;
                timeLastThrew = Time.time;

                // Reset the input state so we don't throw multiple times on one long input
                pickupInputActive = false;
                justPickedUp = false;  // Reset the flag after throwing
            }
        }
        else if (pickupInputActive) {
            // Pickup input has ended
            pickupInputActive = false;

            // Drop the object if held for less than 0.5 seconds and it wasn't just picked up
            if (isHoldingObject && !justPickedUp && Time.time - pickupInputStartTime < throwItemInputHoldThreshold) {
                isHoldingObject = false;
                pickedupObject.Drop(this);
                pickedupObject = null;
            }

            // Reset the justPickedUp flag once the input ends
            justPickedUp = false;
        }
    }
    private void HandleHoverObjects() {
        //RaycastHit[] raycastHits = physics.ConeCastAll(lookingRaycastPositionTransform.position, 1.5f, transform.forward, 2f, 50f);
        RaycastHit[] raycastHits = coneCastHelper.ConeCast(lookingRaycastPositionTransform.position, transform.forward, raycastDistance);
        if (debugVisualizeRays) {
            foreach (var hit in raycastHits) {
                Debug.DrawLine(lookingRaycastPositionTransform.position, hit.point, Color.red);
            }
        }

        foreach (RaycastHit hit in raycastHits) {
            PickupableObject pickupableObject = hit.collider.GetComponentInParent<PickupableObject>();
            if (pickupableObject != null && !pickupableObject.IsPickedUp) {
                pickupableObject.HoverOver(this);
                hoveringObject = pickupableObject;
                return;
            }
        }
        hoveringObject = null;
    }
    //private void HandleCameraZoom() {
    //    float zoomInput = InputManager.Instance.GetCameraZoomInputDelta();

    //    zoomInputs.Enqueue(zoomInput);
    //    if (zoomInputs.Count > bufferSize) {
    //        zoomInputs.Dequeue();
    //    }

    //    float averageZoomInput = GetAverageZoomInput();

    //    float targetZoom = Mathf.Clamp(cameraZoom - averageZoomInput, minZoom, maxZoom);
    //    cameraZoom = Mathf.Lerp(cameraZoom, targetZoom, zoomSpeed * Time.deltaTime);

    //    Vector3 relativeFollowOffset = new Vector3(0, cameraZoom, CameraZoomZFunction(cameraZoom));
    //    Quaternion rotation = Quaternion.FromToRotation(Vector3.up, transform.up);

    //    // Rotate the relative position by this quaternion to align it with the player's up vector
    //    Vector3 adjustedPosition = rotation * relativeFollowOffset;

    //    virtualCamera.GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = adjustedPosition;
    //    //virtualCamera.transform.rotation = Quaternion.Euler(CameraRotationXFunction(cameraZoom), 0f, 0f);
    //}
    //private float GetAverageZoomInput() {
    //    float sum = 0f;
    //    foreach (float input in zoomInputs) {
    //        sum += input;
    //    }
    //    return sum / zoomInputs.Count;
    //}
    private float CameraZoomZFunction(float y) {
        return (0.1375f * y * y) - (2.149f * y) + 4.196f;
    }
    private float CameraRotationXFunction(float y) {
        return (0.6286f * y * y) - (7.124f * y) + 78.95f;
    }
    public Transform GetHoldingObjectSpotTransform() {
        return holdingObjectTransform;
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
