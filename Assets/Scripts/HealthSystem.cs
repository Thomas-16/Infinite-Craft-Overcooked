using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public readonly float MAX_HEALTH = 100f;

    private float health;
    private UIPanel healthBarPanel;
    private HealthBar healthBar;

    [SerializeField] private UIPanel healthBarPrefab;
    [SerializeField] private Transform referencePointTransform;
    [SerializeField] private float healthBarOffset = 2.545f;

    private void Awake() {
        health = MAX_HEALTH;
    }
    private void Start() {
        SetupHealthBar();
    }

    public void Damage(float damage) {
        health -= damage;
        health = Mathf.Clamp(health, 0, MAX_HEALTH);

        healthBar.UpdateHealthBar(health / MAX_HEALTH);
    }
    private void SetupHealthBar() {
        if (UIManager.Instance != null) {
            healthBarPanel = UIManager.Instance.CreateWorldPositionedPanel(
                referencePointTransform == null ? transform : referencePointTransform,
                healthBarPrefab,
                new Vector3(0, healthBarOffset, 0)
            );

            healthBar = healthBarPanel.GetComponent<HealthBar>();
        }
    }
}
