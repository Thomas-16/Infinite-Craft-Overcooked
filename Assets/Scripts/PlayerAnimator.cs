using ECM2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Character character;
    private Animator animator;


    private void Awake() {
        character = GetComponentInParent<Character>();
        animator = GetComponent<Animator>();
    }
    private void Start() {
        character.Jumped += OnPlayerJumped;
    }
    private void OnDestroy() {
        character.Jumped -= OnPlayerJumped;
    }
    private void Update() {
        Vector3 velocity = character.velocity;
        velocity.y = 0;

        if (velocity.magnitude > 0.01f) {
            animator.SetBool("IsWalking", true);
        } else {
            animator.SetBool("IsWalking", false);
        }

        animator.SetBool("IsCrouched", character.IsCrouched());
    }
    private void OnPlayerJumped() {
        animator.SetTrigger("Jump");
    }
}
