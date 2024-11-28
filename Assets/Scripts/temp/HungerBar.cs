using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HungerBar : MonoBehaviour
{
	[SerializeField] private Image fillBar;
	[SerializeField] private Color fullColor = Color.green;
	[SerializeField] private Color emptyColor = Color.red;
	[SerializeField] private float fadeSpeed = 1f;

	private float targetAlpha = 0f;
	private float currentAlpha = 0f;

	private void Awake()
	{
		SetupHungerBar();

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

	private void SetupHungerBar()
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
	}

	public void UpdateHungerBar(float fillAmount)
	{
		if (fillBar != null)
		{
			fillBar.fillAmount = fillAmount;

			// Update fill color while maintaining current alpha
			Color newColor = Color.Lerp(emptyColor, fullColor, fillAmount);
			newColor.a = currentAlpha;
			fillBar.color = newColor;

			// Show the bar
			targetAlpha = 1f;
		}
	}
}