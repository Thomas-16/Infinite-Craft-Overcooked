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

    public event Action<float> OnHealthChanged;

    [SerializeField] private UIPanel healthBarPrefab;
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
        OnHealthChanged?.Invoke(health);
    }
    private void SetupHealthBar() {
        if (UIManager.Instance != null) {
            healthBarPanel = UIManager.Instance.CreateWorldPositionedPanel(
                transform,
                healthBarPrefab,
                new Vector3(0, healthBarOffset, 0)
            );

            healthBar = healthBarPanel.GetComponent<HealthBar>();
        }
    }
}
