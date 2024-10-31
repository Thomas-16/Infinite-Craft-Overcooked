using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class LLement : PickupableObject
{
	[field: SerializeField]
	public string ElementName { get; private set; }

	[Header("Merge Settings")]
	[SerializeField] private float mergeForceThreshold = 5f;
	[SerializeField] private float mergeCheckDelay = 0.1f;

	[Header("Visual Settings")]
	[SerializeField] private SpriteRenderer emojiRenderer;
	[SerializeField] private float defaultScale = 1f;
	[SerializeField] private float spriteSizeMultiplier = 0.3f;

	[Header("UI Settings")]
	[SerializeField] private Canvas worldSpaceCanvas;
	[SerializeField] private TextMeshProUGUI nameLabel;
	[SerializeField] private float labelVerticalOffset = 1f;
	[SerializeField] private Color labelColor = Color.white;
	[SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.5f);

	private ObjectMetadata metadata;
	private bool canTriggerMerge = false;
	private bool hasBeenHeld = false;
	public GameObject visuals;

	private void Awake()
	{
		SetupVisuals();
		if (!string.IsNullOrEmpty(ElementName))
		{
			SetElementName(ElementName);
		}
	}

	private void SetupVisuals()
	{
		// Check if visuals already exists
		if (visuals != null) return;

		// Find existing visuals object if it exists
		Transform existingVisuals = transform.Find("Visuals");
		if (existingVisuals != null)
		{
			visuals = existingVisuals.gameObject;
			// Find existing emoji renderer if it exists
			emojiRenderer = visuals.GetComponentInChildren<SpriteRenderer>();
			return;
		}

		// Create new visuals if none exist
		visuals = new GameObject("Visuals");
		visuals.transform.SetParent(transform);
		visuals.transform.localPosition = Vector3.zero;
		visuals.transform.localRotation = Quaternion.identity;

		visuals.AddComponent<GameBillboard>();

		GameObject emojiObj = new GameObject("EmojiSprite");
		emojiObj.transform.SetParent(visuals.transform);
		emojiObj.transform.localPosition = Vector3.zero;

		SpriteRenderer spriteRenderer = emojiObj.AddComponent<SpriteRenderer>();
		spriteRenderer.sortingOrder = 1;
		emojiRenderer = spriteRenderer;

		SetupUI();
	}

	private void SetupUI()
	{
		if (worldSpaceCanvas == null)
		{
			// Create canvas
			GameObject canvasObj = new GameObject("NameCanvas");
			canvasObj.transform.SetParent(visuals.transform);
			canvasObj.transform.localPosition = Vector3.up * labelVerticalOffset;
			canvasObj.transform.localRotation = Quaternion.identity;

			Canvas canvas = canvasObj.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.WorldSpace;
			canvas.worldCamera = Camera.main;

			// Add constant scale
			canvasObj.AddComponent<ConstantScale>();

			// Create background panel
			GameObject panelObj = new GameObject("BackgroundPanel");
			panelObj.transform.SetParent(canvasObj.transform);

			RectTransform panelRect = panelObj.AddComponent<RectTransform>();
			panelRect.anchoredPosition = Vector2.zero;

			// Add ContentSizeFitter to panel
			ContentSizeFitter panelFitter = panelObj.AddComponent<ContentSizeFitter>();
			panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			panelFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

			// Add HorizontalLayoutGroup to handle padding
			HorizontalLayoutGroup layoutGroup = panelObj.AddComponent<HorizontalLayoutGroup>();
			layoutGroup.padding = new RectOffset(10, 10, 5, 5); // Horizontal and vertical padding
			layoutGroup.childAlignment = TextAnchor.MiddleCenter;

			Image panelImage = panelObj.AddComponent<Image>();
			panelImage.color = panelColor;

			// Create name label
			GameObject labelObj = new GameObject("NameLabel");
			labelObj.transform.SetParent(panelObj.transform);

			RectTransform labelRect = labelObj.AddComponent<RectTransform>();
			labelRect.anchoredPosition = Vector2.zero;

			TextMeshProUGUI tmpText = labelObj.AddComponent<TextMeshProUGUI>();
			tmpText.alignment = TextAlignmentOptions.Center;
			tmpText.fontSize = 20;
			tmpText.color = labelColor;

			worldSpaceCanvas = canvas;
			nameLabel = tmpText;

			// Set initial height for the panel
			LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
			panelRect.sizeDelta = new Vector2(0, 30); // Height only, width will be determined by content
		}
	}

	public async void SetElementName(string elementName)
	{
		ElementName = elementName;

		if (nameLabel != null)
		{
			nameLabel.text = elementName;
		}

		metadata = await ObjectMetadataAPI.Instance.GetObjectMetadata(elementName);

		if (metadata != null)
		{
			Sprite emojiSprite = await EmojiConverter.GetEmojiSprite(metadata.emoji);
			if (emojiSprite != null)
			{
				emojiRenderer.sprite = emojiSprite;
				AdjustSpriteScale();
			}

			if (TryGetComponent<Rigidbody>(out Rigidbody rb))
			{
				//rb.mass = metadata.mass;
			}
		}
	}

	private void AdjustSpriteScale()
	{
		if (emojiRenderer == null || emojiRenderer.sprite == null) return;

		// Get the collider (assuming first collider is the main one)
		Collider mainCollider = mainColliders[0];
		if (mainCollider == null) return;

		// Get world space bounds
		Bounds bounds = mainCollider.bounds;

		// Get the smallest dimension in world space
		float smallestDimension = Mathf.Min(bounds.size.x, bounds.size.y);

		// Calculate local scale needed
		float worldToLocalScale = 1f / transform.lossyScale.x; // Assuming uniform scale
		float targetLocalScale = smallestDimension * worldToLocalScale * spriteSizeMultiplier;

		// Apply the scale to the emoji object's transform
		emojiRenderer.transform.localScale = Vector3.one * targetLocalScale;
	}

	public override void Pickup(Player player)
	{
		base.Pickup(player);
		hasBeenHeld = true;
		canTriggerMerge = false;
	}

	public override void Drop(Player player)
	{
		base.Drop(player);
		StartCoroutine(EnableMergeAfterDelay());
	}

	private IEnumerator EnableMergeAfterDelay()
	{
		yield return new WaitForSeconds(mergeCheckDelay);
		canTriggerMerge = true;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!canTriggerMerge || !hasBeenHeld) return;

		LLement otherElement = collision.gameObject.GetComponent<LLement>();
		if (otherElement != null)
		{
			float collisionForce = collision.impulse.magnitude;

			if (collisionForce >= mergeForceThreshold)
			{
				canTriggerMerge = false;
				otherElement.canTriggerMerge = false;
				GameManager.Instance.MergeElements(this, otherElement);
			}
		}
	}

	private void OnValidate()
	{
		// Only setup if we don't have visuals yet or if we're missing required components
		if (visuals == null || emojiRenderer == null)
		{
			SetupVisuals();
		}

		// Adjust existing components if needed
		if (emojiRenderer != null && emojiRenderer.sprite != null)
		{
			AdjustSpriteScale();
		}
	}

	private void OnDestroy()
	{
		if (worldSpaceCanvas != null)
		{
			Destroy(worldSpaceCanvas.gameObject);
		}
	}
}