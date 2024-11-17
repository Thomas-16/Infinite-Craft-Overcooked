using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIPanel : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Image backgroundPanel;
	[SerializeField] private TextMeshProUGUI textDisplay;

	[Header("Layout Settings")]
	[SerializeField] private float horizontalPadding = 10f;
	[SerializeField] private float verticalPadding = 10f;
	[SerializeField] private float minWidth = 60f;
	[SerializeField] private float minHeight = 30f;

	public RectTransform RectTransform { get; private set; }
	private RectTransform textRectTransform;

	public bool ShouldBeActive = true;

	private void Awake()
	{
		ShouldBeActive = true;

        RectTransform = GetComponent<RectTransform>();
		if (textDisplay != null)
		{
			textRectTransform = textDisplay.GetComponent<RectTransform>();
		}
		ValidateComponents();
		transform.SetAsFirstSibling();
	}

	private void ValidateComponents()
	{
		if (backgroundPanel == null)
		{
			Debug.LogError($"Background panel not assigned on {gameObject.name}!", this);
		}

		if (textDisplay == null)
		{
			Debug.LogError($"Text display not assigned on {gameObject.name}!", this);
		}
	}

	public void SetText(string text)
	{
		if (textDisplay != null)
		{
			textDisplay.text = text;
			// Wait a frame for ContentSizeFitter to update
			Canvas.ForceUpdateCanvases();
			UpdatePanelSize();
		}
	}

	public void UpdatePanelSize()
	{
		if (textDisplay == null || backgroundPanel == null || textRectTransform == null) return;

		// Force layout update
		LayoutRebuilder.ForceRebuildLayoutImmediate(textRectTransform);

		// Get the actual size of the text object after ContentSizeFitter has done its work
		Vector2 textSize = textRectTransform.rect.size;

		// Calculate final size with padding
		Vector2 finalSize = new Vector2(
			Mathf.Max(textSize.x + (horizontalPadding * 2), minWidth),
			Mathf.Max(textSize.y + (verticalPadding * 2), minHeight)
		);

		// Update the background panel and container sizes
		backgroundPanel.rectTransform.sizeDelta = finalSize;
		RectTransform.sizeDelta = finalSize;

		//Debug.Log($"Text size: {textSize}, Final panel size: {finalSize}");
	}

	public void SetPanelColor(Color color)
	{
		if (backgroundPanel != null)
		{
			backgroundPanel.color = color;
		}
	}

	public void SetTextColor(Color color)
	{
		if (textDisplay != null)
		{
			textDisplay.color = color;
		}
	}

	public void SetPadding(float horizontal, float vertical)
	{
		horizontalPadding = horizontal;
		verticalPadding = vertical;
		UpdatePanelSize();
	}

	// Listen for RectTransform changes (in case the ContentSizeFitter updates the size)
	private void OnRectTransformDimensionsChange()
	{
		UpdatePanelSize();
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (RectTransform == null)
		{
			RectTransform = GetComponent<RectTransform>();
		}

		if (textDisplay != null && textRectTransform == null)
		{
			textRectTransform = textDisplay.GetComponent<RectTransform>();
		}

		// Update size when values are changed in inspector
		UnityEditor.EditorApplication.delayCall += () =>
		{
			if (this != null && enabled && gameObject.activeInHierarchy)
			{
				Canvas.ForceUpdateCanvases();
				UpdatePanelSize();
			}
		};
	}
#endif
}