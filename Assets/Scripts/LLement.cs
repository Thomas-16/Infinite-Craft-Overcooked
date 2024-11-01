using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System;

public class LLement : PickupableObject
{
	[field: SerializeField]
	public string ElementName { get; private set; }

	[Header("Merge Settings")]
	[SerializeField] private float mergeForceThreshold = 5f;
	[SerializeField] private float mergeCheckDelay = 0.1f;

	[Header("Visual Settings")]
	[SerializeField] private SpriteRenderer emojiRenderer;
	[SerializeField] private float spriteSizeMultiplier = 0.3f;

	[Header("UI Settings")]
	[SerializeField] private Canvas worldSpaceCanvas;
	[SerializeField] private TextMeshProUGUI nameLabel;
	[SerializeField] private float labelVerticalOffset = 1f;
	[SerializeField] private Color labelColor = Color.white;
	[SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.5f);

	[Header("UI Fade Settings")]
	[SerializeField] private float fadeSpeed = 8f;
	[SerializeField] private float fadeInAlpha = 1f;
	[SerializeField] private float fadeOutAlpha = 0f;

	[Header("Object Settings")]
	[SerializeField] private BoxCollider boxCollider;
	[SerializeField] private float baseSize = 1f;
	[SerializeField] private float spriteScale = 2f;

	private ObjectMetadata metadata;
	private bool canTriggerMerge = false;
	private bool hasBeenHeld = false;
	private bool isMouseOver = false;
	private float currentAlpha = 0f;
	private float targetAlpha = 0f;
	private Vector3 originalScale;

	public GameObject visuals;

	private void Awake()
	{
		originalScale = transform.localScale;
		SetupComponents();
		if (!string.IsNullOrEmpty(ElementName))
		{
			SetElementName(ElementName);
		}
		UpdateUIAlpha(0f);
	}

	private void SetupComponents()
	{
		if (boxCollider == null)
		{
			boxCollider = GetComponent<BoxCollider>();
			if (boxCollider == null)
			{
				boxCollider = gameObject.AddComponent<BoxCollider>();
			}
		}

		SetupVisuals();
		SetupUI();
	}

	protected override void Update()
	{
		base.Update();

		bool shouldBeVisible = HoveringPlayer != null || IsPickedUp || isMouseOver;
		targetAlpha = shouldBeVisible ? fadeInAlpha : fadeOutAlpha;

		if (currentAlpha != targetAlpha)
		{
			currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
			UpdateUIAlpha(currentAlpha);
		}
	}

	private void UpdateUIAlpha(float alpha)
	{
		if (nameLabel != null)
		{
			Color textColor = nameLabel.color;
			textColor.a = alpha;
			nameLabel.color = textColor;

			Image panelImage = nameLabel.transform.parent.GetComponent<Image>();
			if (panelImage != null)
			{
				Color bgColor = panelImage.color;
				bgColor.a = alpha * 0.85f;
				panelImage.color = bgColor;
			}
		}
	}

	private void SetupVisuals()
	{
		if (visuals != null) return;

		Transform existingVisuals = transform.Find("Visuals");
		if (existingVisuals != null)
		{
			visuals = existingVisuals.gameObject;
			emojiRenderer = visuals.GetComponentInChildren<SpriteRenderer>(true); // Include inactive objects in search
			if (emojiRenderer != null)
			{
				emojiRenderer.gameObject.SetActive(true); // Ensure it's active
				Debug.Log($"[LLement] Found existing emoji renderer: {emojiRenderer.gameObject.name}, Active: {emojiRenderer.gameObject.activeInHierarchy}");
			}
			return;
		}

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
		Debug.Log($"[LLement] Created new emoji renderer: {emojiObj.name}, Active: {emojiObj.activeInHierarchy}");
	}

	private void SetupUI()
	{
		if (worldSpaceCanvas == null)
		{
			GameObject canvasObj = new GameObject("NameCanvas");
			canvasObj.transform.SetParent(visuals.transform);
			canvasObj.transform.localPosition = Vector3.up * labelVerticalOffset;
			canvasObj.transform.localRotation = Quaternion.identity;

			Canvas canvas = canvasObj.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.WorldSpace;
			canvas.worldCamera = Camera.main;

			canvasObj.AddComponent<ConstantScale>();

			GameObject panelObj = new GameObject("BackgroundPanel");
			panelObj.transform.SetParent(canvasObj.transform);

			RectTransform panelRect = panelObj.AddComponent<RectTransform>();
			panelRect.anchoredPosition = Vector2.zero;

			ContentSizeFitter panelFitter = panelObj.AddComponent<ContentSizeFitter>();
			panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			panelFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

			HorizontalLayoutGroup layoutGroup = panelObj.AddComponent<HorizontalLayoutGroup>();
			layoutGroup.padding = new RectOffset(10, 10, 5, 5);
			layoutGroup.childAlignment = TextAnchor.MiddleCenter;

			Image panelImage = panelObj.AddComponent<Image>();
			panelImage.color = panelColor;

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

			LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
			panelRect.sizeDelta = new Vector2(0, 30);

			UpdateUIAlpha(fadeOutAlpha);
		}
	}

	public async void SetElementName(string elementName)
	{
		Debug.Log($"[LLement] Setting element name to: {elementName}");
		ElementName = elementName;

		if (nameLabel != null)
		{
			nameLabel.text = elementName;
		}

		try
		{
			metadata = await ObjectMetadataAPI.Instance.GetObjectMetadata(elementName);

			if (metadata != null)
			{
				if (!string.IsNullOrEmpty(metadata.emoji))
				{
					Sprite emojiSprite = await EmojiConverter.GetEmojiSprite(metadata.emoji);
					if (emojiSprite != null)
					{
						if (emojiRenderer != null)
						{
							emojiRenderer.gameObject.SetActive(true); // Ensure it's active before setting sprite
							emojiRenderer.sprite = emojiSprite;
							Debug.Log($"[LLement] Set sprite for {elementName}, Renderer active: {emojiRenderer.gameObject.activeInHierarchy}");
						}
						else
						{
							Debug.LogError($"[LLement] Emoji renderer is null when trying to set sprite for {elementName}");
						}
					}
				}

				ApplyMetadataScale();
				AdjustSpriteToCollider();
			}
			else
			{
				Debug.LogError($"[LLement] Failed to get metadata for {elementName}");
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"[LLement] Error setting element name {elementName}: {e.Message}");
		}
	}

	private void ApplyMetadataScale()
	{
		if (metadata == null) return;

		//float newSize = baseSize * metadata.scale;

		boxCollider.size = Vector3.one;
		//boxCollider.center = Vector3.zero;

		transform.localScale = Vector3.one * GameManager.Instance.SizeConverter(metadata.scale);
	}

	private void AdjustSpriteToCollider()
	{
		if (emojiRenderer == null || emojiRenderer.sprite == null || boxCollider == null)
		{
			Debug.LogError($"[LLement] Missing components in AdjustSpriteToCollider - Renderer: {emojiRenderer != null}, Sprite: {emojiRenderer?.sprite != null}, Collider: {boxCollider != null}");
			return;
		}

		// Ensure the renderer's GameObject is active
		if (!emojiRenderer.gameObject.activeInHierarchy)
		{
			emojiRenderer.gameObject.SetActive(true);
			Debug.Log($"[LLement] Activated emoji renderer in AdjustSpriteToCollider");
		}

		Vector3 colliderSize = boxCollider.size;
		float smallestDimension = Mathf.Min(colliderSize.x, colliderSize.y);

		float targetSize = smallestDimension * spriteScale;

		Vector2 spriteSize = emojiRenderer.sprite.bounds.size;
		float maxSpriteSize = Mathf.Max(spriteSize.x, spriteSize.y);

		float scaleFactor = targetSize / maxSpriteSize;

		emojiRenderer.transform.localScale = Vector3.one * scaleFactor;
		Debug.Log($"[LLement] Adjusted sprite scale to {scaleFactor}, Renderer active: {emojiRenderer.gameObject.activeInHierarchy}");
	}

	private void OnMouseEnter()
	{
		isMouseOver = true;
	}

	private void OnMouseExit()
	{
		isMouseOver = false;
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
		if (boxCollider == null)
		{
			boxCollider = GetComponent<BoxCollider>();
			if (boxCollider == null)
			{
				boxCollider = gameObject.AddComponent<BoxCollider>();
			}
		}

		// Check for inactive emoji renderer
		if (emojiRenderer == null)
		{
			emojiRenderer = GetComponentInChildren<SpriteRenderer>(true); // Include inactive objects
			if (emojiRenderer != null)
			{
				emojiRenderer.gameObject.SetActive(true);
				Debug.Log($"[LLement] Found and activated emoji renderer in OnValidate");
			}
		}

		if (visuals == null || emojiRenderer == null)
		{
			SetupVisuals();
		}

		if (metadata != null)
		{
			ApplyMetadataScale();
			AdjustSpriteToCollider();
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