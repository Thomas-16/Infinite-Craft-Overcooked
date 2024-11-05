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
	[SerializeField] private float floatDistance = 2f; // World space units to float upward

	[Header("Color Settings")]
	[SerializeField] private Color zeroPointsColor = Color.gray;
	[SerializeField] private Color onePointColor = Color.white;
	[SerializeField] private Color twoPointsColor = Color.yellow;
	[SerializeField] private Color threePointsColor = Color.green;

	private Vector3 _worldPosition;
	private RectTransform _targetUI;

	private void Awake()
	{
		RectTransform = GetComponent<RectTransform>();

		if (canvasGroup == null)
			canvasGroup = GetComponent<CanvasGroup>();
	}

	private void LateUpdate()
	{
		if (_worldPosition != Vector3.zero)
		{
			// Update UI position every frame based on world position
			Vector3 screenPos = Camera.main.WorldToScreenPoint(_worldPosition);
			transform.position = screenPos;
		}
	}

	public void Show(Vector3 worldPosition, int points, string reason, RectTransform targetUI)
	{
		_worldPosition = worldPosition;
		_targetUI = targetUI;

		// Set text and color
		pointsText.text = $"+{points}s";
		reasonText.text = reason;
		pointsText.color = GetColorForPoints(points);

		// Initial setup
		transform.localScale = Vector3.zero;
		canvasGroup.alpha = 0f;

		// Animation sequence
		Sequence sequence = DOTween.Sequence();

		// Pop in with bounce
		sequence.Append(transform.DOScale(1f, popInDuration)
			.SetEase(Ease.OutBack, bounceStrength));
		sequence.Join(canvasGroup.DOFade(1f, popInDuration * 0.5f));

		// Float up in world space
		sequence.Append(DOTween.To(() => _worldPosition,
			x => _worldPosition = x,
			_worldPosition + Vector3.up * floatDistance,
			floatDuration)
			.SetEase(Ease.OutQuad));

		// Final move to target UI
		sequence.AppendCallback(() => {
			_worldPosition = Vector3.zero; // Stop world position tracking
		});
		sequence.Append(transform.DOMove(_targetUI.position, floatDuration)
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