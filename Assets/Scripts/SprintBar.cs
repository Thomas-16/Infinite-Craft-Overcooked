using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SprintBar : MonoBehaviour
{
	[SerializeField] private Image fillBar;
	[SerializeField] private Image backgroundBar;  // Reference to background image if you have one
	[SerializeField] private Color fullColor = Color.green;
	[SerializeField] private Color emptyColor = Color.red;
	[SerializeField] private float barWidth = 100f;
	[SerializeField] private float barHeight = 10f;
	[SerializeField] private float fadeSpeed = 3f;

	private float targetAlpha = 0f;
	private float currentAlpha = 0f;

	private void Awake()
	{
		SetupSprintBar();

		// Start hidden
		SetBarAlpha(0f);
	}

	private void Update()
	{
		// Handle fading
		if (currentAlpha != targetAlpha)
		{
			currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
			SetBarAlpha(currentAlpha);
		}
	}

	private void SetupSprintBar()
	{
		if (fillBar != null)
		{
			fillBar.type = Image.Type.Filled;
			fillBar.fillMethod = Image.FillMethod.Horizontal;
			fillBar.fillOrigin = (int)Image.OriginHorizontal.Left;
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

		if (backgroundBar != null)
		{
			Color bgColor = backgroundBar.color;
			bgColor.a = alpha;
			backgroundBar.color = bgColor;
		}
	}

	public void UpdateSprintBar(float fillAmount, bool isSprinting, bool isRecovering)
	{
		if (fillBar != null)
		{
			fillBar.fillAmount = fillAmount;

			// Update fill color while maintaining current alpha
			Color newColor = Color.Lerp(emptyColor, fullColor, fillAmount);
			newColor.a = currentAlpha;
			fillBar.color = newColor;

			// Show the bar if we're sprinting or recovering (not at full)
			bool shouldShow = isSprinting || (isRecovering && fillAmount < 1f);
			targetAlpha = shouldShow ? 1f : 0f;
		}
	}
}