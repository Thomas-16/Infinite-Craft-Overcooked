using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillBar;

    private float targetAlpha = 0f;
    private float currentAlpha = 0f;

    private void Awake() {
        SetupHealthBar();
    }

    private void SetupHealthBar() {
        if (fillBar != null) {
            fillBar.type = Image.Type.Filled;
            fillBar.fillMethod = Image.FillMethod.Horizontal;
            fillBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    public void UpdateHealthBar(float fillAmount) {
        if (fillBar != null) {
            fillBar.fillAmount = fillAmount;
        }
    }
}
