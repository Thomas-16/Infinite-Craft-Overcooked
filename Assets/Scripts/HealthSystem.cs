using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public readonly float MAX_HEALTH = 100f;

    private float health;

    private void Awake() {
        health = MAX_HEALTH;
    }
}
