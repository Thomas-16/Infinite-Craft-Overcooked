using System.Collections;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Seeker), typeof(CharacterController))]
public class Animal : MonoBehaviour
{
    // Movement parameters
    [SerializeField] private float baseSpeed = 1.5f; // Base speed of the animal
    [SerializeField] private float speedVariation = 0.3f; // Random variation in speed
    [SerializeField] private float acceleration = 1.0f; // Acceleration to reach target speed
    [SerializeField] private float deceleration = 1.0f; // Deceleration when stopping
    [SerializeField] private float wanderRadius = 10f; // Radius for wandering around
    [SerializeField] private float stopDistance = 1f; // Distance to stop near waypoints
    [SerializeField] private float idlePauseMin = 1f; // Minimum idle pause duration
    [SerializeField] private float idlePauseMax = 3f; // Maximum idle pause duration
    [SerializeField] private float updateTargetInterval = 2f; // Time between wander target updates

    // A* components
    private Seeker seeker;
    private Path currentPath;
    private int currentWaypoint = 0;

    // CharacterController and AI variables
    private CharacterController characterController;
    private float targetSpeed;
    private float currentSpeed;
    private Vector3 targetPosition;
    private Vector3 velocity = Vector3.zero;
    private float gravity = -9.8f;

    // AI states
    private enum State { Idle, Moving, Turning }
    private State currentState = State.Idle;
    private bool isTransitioning = false;

    private HealthSystem healthSystem;

    private void Start() {
        seeker = GetComponent<Seeker>();
        characterController = GetComponent<CharacterController>();
        healthSystem = GetComponent<HealthSystem>();

        // Randomize target speed slightly for each animal
        targetSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);

        // Start the wandering behavior
        StartCoroutine(WanderBehavior());
    }

    private IEnumerator WanderBehavior() {
        while (true) {
            if (currentState == State.Idle && !isTransitioning) {
                // Decide if animal should wander or pause
                if (Random.value < 0.5f) {
                    // Pause idle behavior
                    isTransitioning = true;
                    yield return new WaitForSeconds(Random.Range(idlePauseMin, idlePauseMax));
                    isTransitioning = false;
                }
                else {
                    // Switch to moving state and select a random wander target
                    isTransitioning = true;
                    currentState = State.Moving;
                    Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                    randomDirection += transform.position;
                    targetPosition = new Vector3(randomDirection.x, transform.position.y, randomDirection.z);
                    StartPath(targetPosition);
                    yield return new WaitForSeconds(updateTargetInterval);
                    isTransitioning = false;
                }
            }
            yield return null;
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

    private void Update() {
        if (currentPath != null && currentWaypoint < currentPath.vectorPath.Count && currentState == State.Moving && !isTransitioning) {
            MoveAlongPath();
        }
    }

    private void MoveAlongPath() {
        if (currentPath == null) return;

        // Calculate direction and distance to the current waypoint
        Vector3 directionToWaypoint = (currentPath.vectorPath[currentWaypoint] - transform.position).normalized;
        float distanceToWaypoint = Vector3.Distance(transform.position, currentPath.vectorPath[currentWaypoint]);

        // Smoothly increase speed to reach target speed
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        // If close enough to waypoint, move to the next or stop
        if (distanceToWaypoint < stopDistance) {
            currentWaypoint++;
            if (currentWaypoint >= currentPath.vectorPath.Count) {
                StartCoroutine(StopAndIdle());
                return;
            }
        }

        // Apply smooth movement toward the waypoint
        Vector3 movement = directionToWaypoint * currentSpeed * Time.deltaTime;

        // Apply gravity
        if (!characterController.isGrounded) {
            velocity.y += gravity * Time.deltaTime;
        }
        else {
            velocity.y = 0;
        }

        movement += velocity * Time.deltaTime;
        characterController.Move(movement);

        // Smoothly rotate toward the waypoint direction
        if (directionToWaypoint != Vector3.zero) {
            Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
            targetRotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * (targetSpeed * 0.5f));
        }
    }

    private IEnumerator StopAndIdle() {
        // Transition to stopping by decelerating speed
        currentState = State.Idle;
        while (currentSpeed > 0) {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
            Vector3 movement = transform.forward * currentSpeed * Time.deltaTime;
            characterController.Move(movement);
            yield return null;
        }
        currentSpeed = 0;
    }
}
