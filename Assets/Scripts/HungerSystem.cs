using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class HungerSystem : MonoBehaviour
{
    public readonly float MAX_HUNGER = 1f;

    private float hunger;
    private UIPanel hungerBarPanel;
    private HungerBar hungerBar;

    [SerializeField] private UIPanel hungerBarPrefab;
    [SerializeField] private Transform referencePointTransform;
    [SerializeField] private float hungerBarOffset = 2.545f;

    private void Awake() {
        hunger = MAX_HUNGER;
    }
    private void Start() {
        SetupHungerBar();
    }

    // percentFull is from 0f to 100f
    public void Starve(float percentFull) {
        if (percentFull < 0f) {
          percentFull = 0f;
        }
        hungerBar.UpdateHungerBar(percentFull / 100 * MAX_HUNGER);
    }
    private void SetupHungerBar() {
        if (UIManager.Instance != null) {
            hungerBarPanel = UIManager.Instance.CreateWorldPositionedPanel(
                referencePointTransform == null ? transform : referencePointTransform,
                hungerBarPrefab,
                new Vector3(0, hungerBarOffset, 0)
            );

            hungerBar = hungerBarPanel.GetComponent<HungerBar>();
        }
    }
}