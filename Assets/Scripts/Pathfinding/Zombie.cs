using System.Collections;
using UnityEngine;
using Pathfinding;

public enum ZombieState { Idle, Chase }

[RequireComponent(typeof(Seeker), typeof(CharacterController))]
public class Zombie : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float chaseUpdateInterval = 1f;
    [SerializeField] private float idleWanderRadius = 5f;
    [SerializeField] private float stopDistance = 1.5f;
    [SerializeField] private float idlePauseDuration = 2f;

    [Header("Attack Parameters")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 10;

    // A* components
    private Seeker seeker;
    private Path currentPath;
    private int currentWaypoint = 0;

    // CharacterController and AI variables
    private CharacterController characterController;
    private float nextPathUpdateTime = 0f;
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private float gravity = -9.8f;

    // State management
    private ZombieState currentState = ZombieState.Chase;

    private bool isIdleMoving = false;
    private float nextAttackTime = 0f; // Cooldown for the next attack

    private HealthSystem healthSystem;

    private void Awake() {
        healthSystem = GetComponent<HealthSystem>();
    }

    void Start() {
        seeker = GetComponent<Seeker>();
        characterController = GetComponent<CharacterController>();

        // Start in idle state
        StartCoroutine(IdleBehavior());
    }

    void Update() {
        switch (currentState) {
            case ZombieState.Idle:
                // Handle Idle behavior (no need for pathfinding in this state)
                break;

            case ZombieState.Chase:
                ChasePlayer();
                break;
        }

        // Move along the current path if one exists
        if (currentPath != null && currentWaypoint < currentPath.vectorPath.Count) {
            MoveAlongPath();
        }

        // Check if the zombie can attack the player
        CheckAttackPlayer();
    }
    public void Damage(float damage) {
        healthSystem.Damage(damage);
    }
    private IEnumerator IdleBehavior() {
        while (currentState == ZombieState.Idle) {
            if (isIdleMoving) {
                // Generate a random point to wander to within a radius
                Vector3 randomDirection = Random.insideUnitSphere * idleWanderRadius;
                randomDirection += transform.position;
                targetPosition = new Vector3(randomDirection.x, transform.position.y, randomDirection.z);

                // Start path to the random idle position
                StartPath(targetPosition);

                // Wait until reaching the target or idle pause duration
                yield return new WaitForSeconds(idlePauseDuration);
                isIdleMoving = false;
            }
            else {
                // Stand still for a bit
                yield return new WaitForSeconds(idlePauseDuration);
                isIdleMoving = true;
            }
        }
    }

    private void ChasePlayer() {
        // Update the path to the player at intervals to avoid constant recalculation
        if (Time.time >= nextPathUpdateTime) {
            targetPosition = GameManager.Instance.Player.transform.position + (Random.insideUnitSphere * 1f); // Add slight randomness
            StartPath(targetPosition);

            // Set the next update time
            nextPathUpdateTime = Time.time + chaseUpdateInterval + Random.Range(-0.3f, 0.3f); // Random variation
        }
    }

    private void StartPath(Vector3 destination) {
        seeker.StartPath(transform.position, destination, OnPathComplete);
    }

    private void OnPathComplete(Path path) {
        if (!path.error) {
            currentPath = path;
            currentWaypoint = 0;
        }
    }

    private void MoveAlongPath() {
        if (currentPath == null) return;

        // Check if we are close enough to the current waypoint
        Vector3 directionToWaypoint = (currentPath.vectorPath[currentWaypoint] - transform.position).normalized;
        float distanceToWaypoint = Vector3.Distance(transform.position, currentPath.vectorPath[currentWaypoint]);

        if (distanceToWaypoint < stopDistance) {
            currentWaypoint++;
        }

        // Apply movement using CharacterController
        Vector3 movement = directionToWaypoint * (currentState == ZombieState.Idle ? speed / 2f : speed) * Time.deltaTime;

        // Apply gravity
        if (!characterController.isGrounded) {
            velocity.y += gravity * Time.deltaTime;
        }
        else {
            velocity.y = 0;
        }

        movement += velocity * Time.deltaTime;
        characterController.Move(movement);

        // Rotate towards the next waypoint
        if (directionToWaypoint != Vector3.zero) {
            Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
            targetRotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
        }
    }

    private void CheckAttackPlayer() {
        // Check distance to the player
        GameObject player = GameManager.Instance.Player.gameObject;
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // Attack if within range and cooldown has elapsed
        if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime) {
            nextAttackTime = Time.time + attackCooldown; // Reset the cooldown
            AttackPlayer();
        }
    }

    private void AttackPlayer() {
        GameManager.Instance.Player.Damage(attackDamage);
        Debug.Log("attacked player", this);
    }

    // Method to manually switch behavior states
    public void SetState(ZombieState newState) {
        if (newState != currentState) {
            currentState = newState;
            if (currentState == ZombieState.Idle) {
                StopAllCoroutines();
                StartCoroutine(IdleBehavior());
            }
            else if (currentState == ZombieState.Chase) {
                StopAllCoroutines();
                nextPathUpdateTime = 0f; // Ensure immediate path update
            }
        }
    }
}
