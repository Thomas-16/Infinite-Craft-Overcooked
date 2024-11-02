using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using UnityEngine.UI;
using System.Threading.Tasks;
using DG.Tweening;

public class GodMessages : MonoBehaviour
{
	public static GodMessages Instance { get; private set; }

	[Header("References")]
	[SerializeField] private UIPanel messageUIPrefab;
	[SerializeField] private UIPanel goalPanelPrefab;
	[SerializeField] private RectTransform messageContainer;
	[SerializeField] private RectTransform goalContainer;

	[Header("Animation Settings")]
	[SerializeField] private float fadeInDuration = 0.5f;
	[SerializeField] private float fadeOutDuration = 0.5f;
	[SerializeField] private float messageDisplayDuration = 4f;
	[SerializeField] private float letterAnimationInterval = 0.05f;
	[SerializeField] private float goalPanelDelay = 1f;

	[Header("Layout Settings")]
	[SerializeField] private float topMargin = 100f;
	[SerializeField] private float leftMargin = 100f;

	[Header("Message Colors")]
	[SerializeField] private Color defaultTextColor = Color.white;
	[SerializeField] private Color acceptedOfferingColor = new Color(0.1f, 0.3f, 0.1f, 0.8f);
	[SerializeField] private Color rejectedOfferingColor = new Color(0.3f, 0.1f, 0.1f, 0.8f);
	[SerializeField] private Color initialMessageColor = new Color(0.2f, 0, 0, 0.8f);
	[SerializeField] private Color goalPanelColor = new Color(0.1f, 0.1f, 0.2f, 0.9f);

	// God personality settings
	private string godPreferences;
	private bool isInitialized = false;

	private Queue<GodMessage> messageQueue = new Queue<GodMessage>();
	private bool isProcessingQueue;
	private UIPanel currentMessagePanel;
	private UIPanel goalPanel;
	private Coroutine currentTextAnimation;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		InitializeMessageContainer();
	}

	private void InitializeMessageContainer()
	{
		if (messageContainer == null || goalContainer == null)
		{
			Debug.LogError("Message containers not assigned to GodMessages!");
			return;
		}

		foreach (Transform child in messageContainer)
		{
			Destroy(child.gameObject);
		}

		foreach (Transform child in goalContainer)
		{
			Destroy(child.gameObject);
		}
	}

	private async void Start()
	{
		await InitializeGodPersonality();
	}

	private async Task InitializeGodPersonality()
	{
		string prompt = "You are an ancient god with very specific preferences for offerings. " +
					   "Describe your preferences for earthly objects in 2-3 sentences. " +
					   "Keep the sentences short. Include a line break after every phrase. " +
					   "Include specific characteristics you love and hate. Be creative but consistent. " +
					   "For example: 'I demand offerings of natural timber, \n " +
					   "for they remind me of the sacred groves of my realm. \n " +
					   "I shall smite any who dare present me with processed or artificial materials.'";

		try
		{
			godPreferences = await ChatGPTClient.Instance.SendChatRequest(prompt);
			isInitialized = true;

			QueueMessage("*A divine presence materializes*", defaultTextColor, initialMessageColor);
			await Task.Delay(2000);
			QueueMessage(godPreferences, defaultTextColor, initialMessageColor);

			// After the god declares their preferences, create the goal panel
			await CreateGoalPanel();
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to initialize god personality: {e.Message}");
			godPreferences = "I am a god of simple tastes.\nI shall accept what pleases me.\nDisappoint me at your peril.";
			isInitialized = true;
		}
	}

	private async Task CreateGoalPanel()
	{
		await Task.Delay((int)(goalPanelDelay * 1000));

		string prompt = $"Based on the god's preferences: '{godPreferences}'\n" +
					   "List 2-3 types of objects or items that would please this god.\n" +
					   "Keep each very short (2-4 words).\n" +
					   "Don't use any verbs or full sentences.\n" +
					   "Just list the actual items or materials.\n" +
					   "Example:\n" +
					   "Natural wooden logs\n" +
					   "Moss-covered stones\n" +
					   "Fresh forest mushrooms";

		try
		{
			string rawGoalText = await ChatGPTClient.Instance.SendChatRequest(prompt);

			// Format the goals with bullet points
			string[] goals = rawGoalText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			string formattedGoals = "Goals:\n";
			foreach (string goal in goals)
			{
				if (!string.IsNullOrWhiteSpace(goal))
				{
					formattedGoals += $"• {goal.Trim()}\n";
				}
			}

			// Create and setup goal panel
			goalPanel = Instantiate(goalPanelPrefab, goalContainer);
			RectTransform goalRect = goalPanel.RectTransform;

			// Set anchors for left justification
			goalRect.anchorMin = new Vector2(0, 0.5f);
			goalRect.anchorMax = new Vector2(0, 0.5f);
			goalRect.pivot = new Vector2(0, 0.5f);

			// Position from left edge
			goalRect.anchoredPosition = new Vector2(leftMargin, 0f);

			goalPanel.SetText(formattedGoals.TrimEnd());
			goalPanel.SetTextColor(defaultTextColor);
			goalPanel.SetPanelColor(goalPanelColor);

			// Fade in the goal panel
			CanvasGroup canvasGroup = goalPanel.gameObject.AddComponent<CanvasGroup>();
			canvasGroup.alpha = 0f;

			DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, fadeInDuration)
				.SetEase(Ease.OutQuad);
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to create goal text: {e.Message}");
		}
	}

	public async Task<(bool accepted, string response)> EvaluateOffering(string itemName)
	{
		if (!isInitialized)
		{
			Debug.LogWarning("Attempted to evaluate offering before god preferences were initialized");
			return (false, "The god has not yet awakened...");
		}

		string prompt = $"Based on your stated preferences as a god: '{godPreferences}'\n" +
					   $"Would you accept a {itemName} as an offering? Consider the object's properties and your divine preferences.\n" +
					   $"Reply with ONLY 'YES' or 'NO' followed by a brief one-sentence divine proclamation.";

		try
		{
			string response = await ChatGPTClient.Instance.SendChatRequest(prompt);
			bool isAccepted = response.Trim().StartsWith("YES", StringComparison.OrdinalIgnoreCase);

			Color backgroundColor = isAccepted ? acceptedOfferingColor : rejectedOfferingColor;
			QueueMessage(response, defaultTextColor, backgroundColor);

			return (isAccepted, response);
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to evaluate offering: {e.Message}");
			return (false, "Your offering displeases me through its mere existence.");
		}
	}

	public void QueueMessage(string text, Color textColor, Color backgroundColor)
	{
		GodMessage message = new GodMessage
		{
			Text = text,
			TextColor = textColor,
			BackgroundColor = backgroundColor
		};

		StartCoroutine(DisplayNewMessage(message));
	}

	private IEnumerator DisplayNewMessage(GodMessage newMessage)
	{
		// If there's a current message, fade it out
		if (currentMessagePanel != null)
		{
			CanvasGroup oldCanvasGroup = currentMessagePanel.GetComponent<CanvasGroup>();
			if (oldCanvasGroup != null)
			{
				// Stop current text animation if it's running
				if (currentTextAnimation != null)
				{
					StopCoroutine(currentTextAnimation);
				}

				// Fade out the old message
				DOTween.To(() => oldCanvasGroup.alpha, x => oldCanvasGroup.alpha = x, 0f, fadeOutDuration)
					.SetEase(Ease.InQuad);

				// Keep reference to destroy after fade
				UIPanel oldPanel = currentMessagePanel;
				yield return new WaitForSeconds(fadeOutDuration);
				if (oldPanel != null)
				{
					Destroy(oldPanel.gameObject);
				}
			}
		}

		// Create and display the new message
		currentMessagePanel = Instantiate(messageUIPrefab, messageContainer);

		// Set anchors for top justification
		RectTransform messageRect = currentMessagePanel.RectTransform;
		messageRect.anchorMin = new Vector2(0.5f, 1f);
		messageRect.anchorMax = new Vector2(0.5f, 1f);
		messageRect.pivot = new Vector2(0.5f, 1f);

		// Position from top edge
		messageRect.anchoredPosition = new Vector2(0f, -topMargin);

		currentMessagePanel.SetText("");
		currentMessagePanel.SetTextColor(newMessage.TextColor);
		currentMessagePanel.SetPanelColor(newMessage.BackgroundColor);

		// Setup fade in
		CanvasGroup canvasGroup = currentMessagePanel.gameObject.AddComponent<CanvasGroup>();
		canvasGroup.alpha = 0f;

		// Fade in new message
		DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, fadeInDuration)
			.SetEase(Ease.OutQuad);

		yield return new WaitForSeconds(fadeInDuration);

		// Start text animation
		currentTextAnimation = StartCoroutine(AnimateText(newMessage.Text));
	}

	private IEnumerator AnimateText(string fullText)
	{
		string currentText = "";

		foreach (char letter in fullText)
		{
			currentText += letter;
			if (currentMessagePanel != null)
			{
				currentMessagePanel.SetText(currentText);
			}

			float delay = letter == '.' || letter == '!' || letter == '?' ?
				letterAnimationInterval * 4 : letterAnimationInterval;

			yield return new WaitForSeconds(delay);
		}

		// Wait for display duration after text is complete
		yield return new WaitForSeconds(messageDisplayDuration);

		// Fade out if this is still the current message
		if (currentMessagePanel != null)
		{
			CanvasGroup canvasGroup = currentMessagePanel.GetComponent<CanvasGroup>();
			if (canvasGroup != null)
			{
				DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, fadeOutDuration)
					.SetEase(Ease.InQuad);

				yield return new WaitForSeconds(fadeOutDuration);

				if (currentMessagePanel != null)
				{
					Destroy(currentMessagePanel.gameObject);
					currentMessagePanel = null;
				}
			}
		}
	}

	public void ClearMessages()
	{
		if (currentTextAnimation != null)
		{
			StopCoroutine(currentTextAnimation);
		}

		if (currentMessagePanel != null)
		{
			Destroy(currentMessagePanel.gameObject);
			currentMessagePanel = null;
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private class GodMessage
	{
		public string Text { get; set; }
		public Color TextColor { get; set; }
		public Color BackgroundColor { get; set; }
	}
}