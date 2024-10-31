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

	[Header("UI Fade Settings")]
	[SerializeField] private float fadeSpeed = 8f;
	[SerializeField] private float fadeInAlpha = 1f;
	[SerializeField] private float fadeOutAlpha = 0f;

	private ObjectMetadata metadata;
	private bool canTriggerMerge = false;
	private bool hasBeenHeld = false;
	private bool isMouseOver = false;
	private float currentAlpha = 0f;
	private float targetAlpha = 0f;

	public GameObject visuals;

	private void Awake()
	{
		SetupVisuals();
		if (!string.IsNullOrEmpty(ElementName))
		{
			SetElementName(ElementName);
		}
		UpdateUIAlpha(0f);
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
			emojiRenderer = visuals.GetComponentInChildren<SpriteRenderer>();
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

		SetupUI();
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

		Collider mainCollider = mainColliders[0];
		if (mainCollider == null) return;

		Bounds bounds = mainCollider.bounds;
		float smallestDimension = Mathf.Min(bounds.size.x, bounds.size.y);
		float worldToLocalScale = 1f / transform.lossyScale.x;
		float targetLocalScale = smallestDimension * worldToLocalScale * spriteSizeMultiplier;

		emojiRenderer.transform.localScale = Vector3.one * targetLocalScale;
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
		if (visuals == null || emojiRenderer == null)
		{
			SetupVisuals();
		}

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
