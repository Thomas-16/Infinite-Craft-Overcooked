using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HungerBar : MonoBehaviour
{
    [SerializeField] private Image fillBar;

    private float targetAlpha = 0f;
    private float currentAlpha = 0f;

    private void Awake() {
        SetupHungerBar();
    }

    private void SetupHungerBar() {
        if (fillBar != null) {
            fillBar.type = Image.Type.Filled;
            fillBar.fillMethod = Image.FillMethod.Horizontal;
            fillBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    public void UpdateHungerBar(float fillAmount) {
        if (fillBar != null) {
            //UnityEngine.Debug.Log("Hunger bar amt: " + fillAmount);
            fillBar.fillAmount = fillAmount;
        }
    }
}
