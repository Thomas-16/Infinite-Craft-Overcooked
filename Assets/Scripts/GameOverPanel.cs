// GameOverPanel.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class GameOverPanel : UIPanel
{
	[SerializeField] private TextMeshProUGUI gameOverText;
	[SerializeField] private Button restartButton;
	[SerializeField] private CanvasGroup canvasGroup;
	[SerializeField] private float fadeInDuration = 1f;

	private void Start()
	{
		restartButton.onClick.AddListener(GameManager.Instance.RestartGame);
	}

	public System.Collections.IEnumerator FadeIn()
	{
		canvasGroup.gameObject.SetActive(true);
		canvasGroup.alpha = 0f;

		float elapsed = 0f;
		while (elapsed < fadeInDuration)
		{
			elapsed += Time.deltaTime;
			canvasGroup.alpha = elapsed / fadeInDuration;
			yield return null;
		}
		canvasGroup.alpha = 1f;
	}

	public System.Collections.IEnumerator FadeOut()
	{
		canvasGroup.alpha = 0f;
		float elapsed = 0f;
		while (elapsed < fadeInDuration)
		{
			elapsed += Time.deltaTime;
			canvasGroup.alpha = 1 - elapsed / fadeInDuration;
			yield return null;
		}
		canvasGroup.alpha = 0f;
		canvasGroup.gameObject.SetActive(false);

	}
}