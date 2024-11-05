using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class TutorialPanel : UIPanel
{
	[Header("Tutorial Settings")]
	[SerializeField] private float fadeDuration = 0.5f;
	[SerializeField] private CanvasGroup canvasGroup;
	[SerializeField] private Image tutorialImage;
	[SerializeField] private float imageSize = 100f;
	[SerializeField] private float imageSpacing = 10f; // Space between image and text

	private bool hasMoved = false;
	private Transform playerTransform;
	private Vector3 lastPosition;

	protected void Start()
	{
		// Get references
		if (canvasGroup == null)
			canvasGroup = gameObject.AddComponent<CanvasGroup>();

		playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
		if (playerTransform != null)
			lastPosition = playerTransform.position;

		// Set initial text
		//SetText("Combine words together to keep the timer up!");

		// Set up the image
		if (tutorialImage != null)
		{
			// Set image size
			//tutorialImage.rectTransform.sizeDelta = new Vector2(imageSize, imageSize);

			// Position image above text
			//tutorialImage.rectTransform.anchoredPosition = new Vector2(0, imageSpacing);

			// Update panel size to accommodate image
			//UpdatePanelSize();
		}

		// Center the panel
		RectTransform.anchoredPosition = Vector2.zero;
	}

	private void Update()
	{
		if (!hasMoved && playerTransform != null)
		{
			// Check if player has moved
			if (Vector3.Distance(lastPosition, playerTransform.position) > 0.1f)
			{
				hasMoved = true;
				FadeOut();
			}
		}
	}

	private void FadeOut()
	{
		// Fade out panel and image using DOTween
		canvasGroup.DOFade(0f, fadeDuration)
			.OnComplete(() => Destroy(gameObject));
	}

	// Override the base UpdatePanelSize to account for the image
	public override void UpdatePanelSize()
	{
		if (textDisplay == null || backgroundPanel == null) return;

		// Force layout update for text
		LayoutRebuilder.ForceRebuildLayoutImmediate(textDisplay.rectTransform);

		// Get the text size
		Vector2 textSize = textDisplay.rectTransform.rect.size;

		// Calculate total height including image if present
		float totalHeight = textSize.y;
		float totalWidth = textSize.x;

		if (tutorialImage != null && tutorialImage.gameObject.activeInHierarchy)
		{
			totalHeight += imageSize + imageSpacing;
			totalWidth = Mathf.Max(totalWidth, imageSize);
		}

		// Calculate final size with padding
		Vector2 finalSize = new Vector2(
			Mathf.Max(totalWidth + (horizontalPadding * 2), minWidth),
			Mathf.Max(totalHeight + (verticalPadding * 2), minHeight)
		);

		// Update the background panel and container sizes
		backgroundPanel.rectTransform.sizeDelta = finalSize;
		RectTransform.sizeDelta = finalSize;
	}
}