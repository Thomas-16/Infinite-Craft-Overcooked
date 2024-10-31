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
	[SerializeField] private float labelFadeSpeed = 5f;
	[SerializeField] private Color labelColor = Color.white;

	private ObjectMetadata metadata;
	private bool canTriggerMerge = false;
	private bool hasBeenHeld = false;
	private bool isMouseOver = false;
	private float currentLabelAlpha = 0f;
	private float targetLabelAlpha = 0f;

	private void Awake()
	{
		SetupVisuals();
		SetupUI();
		if (!string.IsNullOrEmpty(ElementName))
		{
			SetElementName(ElementName);
		}
	}

	private void SetupVisuals()
	{
		if (emojiRenderer == null)
		{
			// Create a child object for visuals
			GameObject visuals = new GameObject("Visuals");
			visuals.transform.SetParent(transform);
			visuals.transform.localPosition = Vector3.zero;
			visuals.transform.localRotation = Quaternion.identity;

			// Add GameBillboard component
			visuals.AddComponent<GameBillboard>();

			// Add emoji renderer
			GameObject emojiObj = new GameObject("EmojiSprite");
			emojiObj.transform.SetParent(visuals.transform);
			emojiObj.transform.localPosition = Vector3.zero;

			SpriteRenderer spriteRenderer = emojiObj.AddComponent<SpriteRenderer>();
			spriteRenderer.sortingOrder = 1;
			emojiRenderer = spriteRenderer;
		}
	}

	private void SetupUI()
	{
		if (worldSpaceCanvas == null)
		{
			Collider mainCollider = mainColliders[0];
			if (mainCollider == null) return;

			// Get collider bounds for sizing
			Bounds bounds = mainCollider.bounds;
			float colliderWidth = bounds.size.x;

			// Create canvas
			GameObject canvasObj = new GameObject("NameCanvas");
			canvasObj.transform.SetParent(transform);
			canvasObj.transform.localPosition = Vector3.up * (bounds.size.y + labelVerticalOffset * 0.5f);
			canvasObj.transform.localRotation = Quaternion.identity;

			Canvas canvas = canvasObj.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.WorldSpace;
			canvas.worldCamera = Camera.main;

			// Add billboard to canvas
			canvasObj.AddComponent<GameBillboard>();

			// Set canvas size based on collider
			RectTransform canvasRect = canvas.GetComponent<RectTransform>();
			canvasRect.sizeDelta = new Vector2(colliderWidth * 2f, colliderWidth * 0.5f); // Width is double collider, height is half width

			// Create name label
			GameObject labelObj = new GameObject("NameLabel");
			labelObj.transform.SetParent(canvasRect);

			RectTransform labelRect = labelObj.AddComponent<RectTransform>();
			labelRect.anchorMin = Vector2.zero;
			labelRect.anchorMax = Vector2.one;
			labelRect.sizeDelta = Vector2.zero;
			labelRect.anchoredPosition = Vector2.zero;

			TextMeshProUGUI tmpText = labelObj.AddComponent<TextMeshProUGUI>();
			tmpText.alignment = TextAlignmentOptions.Center;
			tmpText.enableAutoSizing = true;
			tmpText.fontSizeMin = 0.1f;
			tmpText.fontSizeMax = 2f;
			tmpText.color = labelColor;

			// Add canvas scaler to maintain consistent size in orthographic view
			CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
			scaler.dynamicPixelsPerUnit = 100f;

			worldSpaceCanvas = canvas;
			nameLabel = tmpText;
		}

		// Ensure the label starts hidden
		if (nameLabel != null)
		{
			Color c = nameLabel.color;
			c.a = 0;
			nameLabel.color = c;

			// Set smaller font size
			AdjustTextScale();
		}
	}

	private void AdjustTextScale()
	{
		if (nameLabel == null) return;

		Collider mainCollider = mainColliders[0];
		if (mainCollider == null) return;

		// Get world space bounds
		Bounds bounds = mainCollider.bounds;

		// Get the width of the collider in world space
		float colliderWidth = bounds.size.x;

		// Calculate scale relative to collider size
		float worldToLocalScale = 1f / transform.lossyScale.x;
		float baseTextSize = colliderWidth * worldToLocalScale;

		// Apply text sizing
		nameLabel.fontSize = baseTextSize * 0.5f; // Adjust multiplier as needed
	}


	private void Update()
	{
		UpdateLabelVisibility();
	}

	private void UpdateLabelVisibility()
	{
		// Set target alpha based on hover states
		targetLabelAlpha = (isMouseOver || HoveringPlayer != null) ? 1f : 0f;

		// Smoothly interpolate current alpha
		currentLabelAlpha = Mathf.Lerp(currentLabelAlpha, targetLabelAlpha, Time.deltaTime * labelFadeSpeed);

		// Update label alpha
		if (nameLabel != null)
		{
			Color c = nameLabel.color;
			c.a = currentLabelAlpha;
			nameLabel.color = c;
		}
	}

	private void OnMouseEnter()
	{
		isMouseOver = true;
	}

	private void OnMouseExit()
	{
		isMouseOver = false;
	}

	public async void SetElementName(string elementName)
	{
		ElementName = elementName;

		if (nameLabel != null)
		{
			nameLabel.text = elementName;
			AdjustTextScale(); // Adjust text scale when setting new name
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

			//transform.localScale = Vector3.one * (defaultScale * metadata.scale);

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
		SetupVisuals();
		if (emojiRenderer != null && emojiRenderer.sprite != null)
		{
			AdjustSpriteScale();
		}
		if (nameLabel != null)
		{
			AdjustTextScale();
		}
	}

	private void OnDestroy()
	{
		// Clean up any UI elements if needed
		if (worldSpaceCanvas != null)
		{
			Destroy(worldSpaceCanvas.gameObject);
		}
	}
}