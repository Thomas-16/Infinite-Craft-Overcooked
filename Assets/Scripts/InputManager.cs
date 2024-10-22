using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance {  get; private set; }
    public PlayerInputActions inputActions;

    private void Awake() {
        Instance = this;
        inputActions = new PlayerInputActions();
        inputActions.Enable();
    }

    public Vector2 GetMovementInputVector() {
        return inputActions.Player.Movement.ReadValue<Vector2>();
    }

}
