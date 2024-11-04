// PointBubble.cs
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class PointBubble : UIPanel
{
	[Header("References")]
	[SerializeField] private TextMeshProUGUI pointsText;
	[SerializeField] private TextMeshProUGUI reasonText;
	[SerializeField] private CanvasGroup canvasGroup;

	[Header("Animation Settings")]
	[SerializeField] private float popInDuration = 0.3f;
	[SerializeField] private float floatDuration = 1f;
	[SerializeField] private float fadeOutDuration = 0.3f;
	[SerializeField] private float bounceStrength = 1.2f;

	[Header("Color Settings")]
	[SerializeField] private Color zeroPointsColor = Color.gray;
	[SerializeField] private Color onePointColor = Color.white;
	[SerializeField] private Color twoPointsColor = Color.yellow;
	[SerializeField] private Color threePointsColor = Color.green;

	private void Awake()
	{
		if (canvasGroup == null)
			canvasGroup = GetComponent<CanvasGroup>();
	}

	public void Show(Vector3 worldPosition, int points, string reason, RectTransform targetUI)
	{
		// Set text and color
		pointsText.text = $"+{points}s";
		reasonText.text = reason;
		pointsText.color = GetColorForPoints(points);

		// Convert world position to screen position
		Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
		transform.position = screenPos;

		// Initial setup
		transform.localScale = Vector3.zero;
		canvasGroup.alpha = 0f;

		// Animation sequence
		Sequence sequence = DOTween.Sequence();

		// Pop in with bounce
		sequence.Append(transform.DOScale(1f, popInDuration)
			.SetEase(Ease.OutBack, bounceStrength));
		sequence.Join(canvasGroup.DOFade(1f, popInDuration * 0.5f));

		// Small float up
		sequence.Append(transform.DOMoveY(transform.position.y + 50f, floatDuration)
			.SetEase(Ease.OutQuad));

		// Move to timer
		sequence.Append(transform.DOMove(targetUI.position, floatDuration)
			.SetEase(Ease.InOutQuad));

		// Fade out
		sequence.Join(canvasGroup.DOFade(0f, fadeOutDuration));
		sequence.Join(transform.DOScale(0.5f, fadeOutDuration));

		// Cleanup
		sequence.OnComplete(() => Destroy(gameObject));
	}

	private Color GetColorForPoints(int points)
	{
		return points switch
		{
			0 => zeroPointsColor,
			1 => onePointColor,
			2 => twoPointsColor,
			_ => threePointsColor
		};
	}
}