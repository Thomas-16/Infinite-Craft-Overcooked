using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerPanel : UIPanel
{
	[Header("Timer UI References")]
	[SerializeField] private Image radialFill;  // Single radial fill image
	[SerializeField] private TextMeshProUGUI timerText;

	public void UpdateTimerText(string text)
	{
		if (timerText != null)
		{
			timerText.text = text;
		}
	}

	public void UpdateRadialFill(float fillAmount)
	{
		if (radialFill != null)
		{
			radialFill.fillAmount = fillAmount;
		}
	}
}