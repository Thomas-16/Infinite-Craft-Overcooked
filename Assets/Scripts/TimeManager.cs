// TimerManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TimerManager : MonoBehaviour
{
	public static TimerManager Instance { get; private set; }

	[Header("Timer Settings")]
	[SerializeField] private float totalTimeInMinutes = 1f;
	[SerializeField] private float warningThreshold = 0.3f;

	[Header("UI Settings")]
	//[SerializeField] private TimerPanel timerPanelPrefab;
	[SerializeField] private Transform timerAnchor;
	[SerializeField] private Vector3 timerOffset = new Vector3(0, 0, 0);
	[SerializeField] private Color normalColor = Color.white;
	[SerializeField] private Color warningColor = Color.red;
	[SerializeField] private float warningFlashSpeed = 2f;

	private float currentTime;
	private float totalTime;
	private bool isTimerRunning = false;
	[SerializeField] private TimerPanel timerPanel;

	private void Awake()
	{
		Instance = this;
		totalTime = totalTimeInMinutes * 60f;
	}

	private void Start()
	{
		//SetupTimerUI();
		InitializeTimer();
	}

	public RectTransform GetTimerRect()
	{
		return timerPanel.RectTransform;
	}

	/*private void SetupTimerUI()
	{
		if (UIManager.Instance != null && timerPanelPrefab != null)
		{
			timerPanel = UIManager.Instance.CreateWorldPositionedPanel(
				timerAnchor,
				timerPanelPrefab,
				timerOffset
			) as TimerPanel;
		}
	}*/

	private void InitializeTimer()
	{
		currentTime = totalTime;
		UpdateTimerDisplay();
		isTimerRunning = true;
		StartCoroutine(FlashWarningCoroutine());
	}

	private void Update()
	{
		if (!isTimerRunning) return;

		if (currentTime > 0)
		{
			currentTime -= Time.deltaTime;
			UpdateTimerDisplay();
		}
		else
		{
			TimerExpired();
		}
	}

	private void UpdateTimerDisplay()
	{
		if (timerPanel == null) return;

		// Update timer text
		int minutes = Mathf.FloorToInt(currentTime / 60f);
		int seconds = Mathf.FloorToInt(currentTime % 60f);
		timerPanel.UpdateTimerText($"{minutes:00}:{seconds:00}");

		// Update radial fill
		float fillAmount = currentTime / totalTime;
		timerPanel.UpdateRadialFill(fillAmount);
	}

	public void AddTime(float secondsToAdd)
	{
		currentTime += secondsToAdd;
		totalTime = Mathf.Max(totalTime, currentTime);
		UpdateTimerDisplay();
	}

	private IEnumerator FlashWarningCoroutine()
	{
		while (isTimerRunning)
		{
			if (currentTime / totalTime < warningThreshold)
			{
				float flashValue = (Mathf.Sin(Time.time * warningFlashSpeed) + 1f) / 2f;
				timerPanel.GetComponent<Image>().color = Color.Lerp(normalColor, warningColor, flashValue);
			}
			else
			{
				timerPanel.GetComponent<Image>().color = normalColor;
			}
			yield return null;
		}
	}

	private void OnDestroy()
	{
		if (timerAnchor != null && UIManager.Instance != null)
		{
			UIManager.Instance.RemoveWorldPositionedPanel(timerAnchor);
		}
	}

	public float GetRemainingTime() => currentTime;
	public void PauseTimer() => isTimerRunning = false;
	public void ResumeTimer() => isTimerRunning = true;

	private void TimerExpired()
	{
		isTimerRunning = false;
		GameManager.Instance.OnGameOver();
	}

	// Add reset method
	public void ResetTimer()
	{
		currentTime = totalTime;
		UpdateTimerDisplay();
		isTimerRunning = true;
	}
}