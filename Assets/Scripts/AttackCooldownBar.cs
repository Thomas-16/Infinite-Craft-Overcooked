using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttackCooldownBar : MonoBehaviour
{
    [SerializeField] private Image fillBar;

    private void Awake() {
        SetupBar();
    }

    private void SetupBar() {
        if (fillBar != null) {
            fillBar.type = Image.Type.Filled;
            fillBar.fillMethod = Image.FillMethod.Horizontal;
            fillBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    public void UpdateBar(float fillAmount) {
        if (fillBar != null) {
            fillBar.fillAmount = fillAmount;
        }
    }
}
