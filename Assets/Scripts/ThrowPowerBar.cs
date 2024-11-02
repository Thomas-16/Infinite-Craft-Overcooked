using UnityEngine;
using UnityEngine.UI;

public class ThrowPowerBar : MonoBehaviour
{
	[SerializeField] private Image fillBar;
	[SerializeField] private float fadeSpeed = 3f;

	[Header("Color Settings")]
	[SerializeField] private Color minChargeColor = Color.yellow;
	[SerializeField] private Color maxChargeColor = new Color(1f, 0.5f, 0f, 1f); // Orange

	private float targetAlpha = 0f;
	private float currentAlpha = 0f;

	private void Awake()
	{
		SetupPowerBar();
		SetBarAlpha(0f);
	}

	private void Update()
	{
		if (currentAlpha != targetAlpha)
		{
			currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
			SetBarAlpha(currentAlpha);
		}
	}

	private void SetupPowerBar()
	{
		if (fillBar != null)
		{
			fillBar.type = Image.Type.Filled;
			fillBar.fillMethod = Image.FillMethod.Horizontal;
			fillBar.fillOrigin = (int)Image.OriginHorizontal.Left;
			fillBar.color = minChargeColor;  // Set initial color
		}
	}

	private void SetBarAlpha(float alpha)
	{
		if (fillBar != null)
		{
			Color fillColor = fillBar.color;
			fillColor.a = alpha;
			fillBar.color = fillColor;
		}
	}

	public void UpdatePowerBar(float fillAmount, bool isCharging)
	{
		if (fillBar != null)
		{
			fillBar.fillAmount = fillAmount;

			// Update color while maintaining current alpha
			Color newColor = Color.Lerp(minChargeColor, maxChargeColor, fillAmount);
			newColor.a = currentAlpha;
			fillBar.color = newColor;

			// Show when charging, hide when not
			targetAlpha = isCharging ? 1f : 0f;
		}
	}
}