using UnityEngine;
using System.Collections;
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
	[SerializeField] private float nameTagOffset = 1.5f;
	[SerializeField] private Color textColor = Color.white;
	[SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.5f);

	[Header("UI Fade Settings")]
	[SerializeField] private bool alwaysShowUI = true;
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
	private UIPanel namePanel;

	public GameObject visuals;

	[SerializeField]
	private UIPanel namePanelPrefab;


	[Header("Animation Settings")]
	[SerializeField] private float scaleInDuration = 0.3f;
	[SerializeField] private float scaleOutDuration = 0.2f;
	[SerializeField] private AnimationCurve scaleInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
	[SerializeField] private AnimationCurve scaleOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
	[SerializeField] private float moveToMergeDuration = 0.3f;
	[SerializeField] private AnimationCurve moveToMergeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

	private bool isScalingIn = false;
	private bool isScalingOut = false;
	private Vector3 targetScale;

	private void Awake()
	{
		originalScale = transform.localScale;
		SetupComponents();
		if (!string.IsNullOrEmpty(ElementName))
		{
			//SetElementName(ElementName);
		}
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

	private void SetupVisuals()
	{
		if (visuals != null) return;

		Transform existingVisuals = transform.Find("Visuals");
		if (existingVisuals != null)
		{
			visuals = existingVisuals.gameObject;
			emojiRenderer = visuals.GetComponentInChildren<SpriteRenderer>(true);
			if (emojiRenderer != null)
			{
				emojiRenderer.gameObject.SetActive(true);
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
		namePanel = UIManager.Instance.CreateWorldPositionedPanel(
			transform,
			namePanelPrefab,
			new Vector3(0, nameTagOffset, 0)
		);

		if (namePanel != null)
		{
			namePanel.SetPanelColor(panelColor);
			namePanel.SetTextColor(textColor);
			UpdateUIAlpha(alwaysShowUI ? fadeInAlpha : fadeOutAlpha);
		}
	}


	protected override void Update()
	{
		base.Update();

		if (!alwaysShowUI)
		{
			bool shouldBeVisible = HoveringPlayer != null || IsPickedUp || isMouseOver;
			targetAlpha = shouldBeVisible ? fadeInAlpha : fadeOutAlpha;

			if (currentAlpha != targetAlpha)
			{
				currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
				UpdateUIAlpha(currentAlpha);
			}
		}
	}

	private void UpdateUIAlpha(float alpha)
	{
		if (namePanel != null)
		{
			Color panelColorWithAlpha = panelColor;
			panelColorWithAlpha.a = alpha * 0.85f;
			namePanel.SetPanelColor(panelColorWithAlpha);

			Color textColorWithAlpha = textColor;
			textColorWithAlpha.a = alpha;
			namePanel.SetTextColor(textColorWithAlpha);
		}
	}

	public async void SetElementName(string elementName, ObjectMetadata data)
	{
		Debug.Log($"[LLement] Setting element name to: {elementName}");
		ElementName = elementName;

		if (namePanel != null)
		{
			namePanel.SetText(elementName);
		}

		Sprite emojiSprite = await EmojiConverter.GetEmojiSprite(data.emoji);
		if (emojiSprite != null)
		{
			if (emojiRenderer != null)
			{
				emojiRenderer.gameObject.SetActive(true);
				emojiRenderer.sprite = emojiSprite;
			}
			else
			{
				Debug.LogError($"[LLement] Emoji renderer is null when trying to set sprite for {elementName}");
			}
		}
		metadata = data;

		ApplyMetadataScale();
		AdjustSpriteToCollider();

		// Start scale in animation
		StartScaleIn();
	}

	private void StartScaleIn()
	{
		targetScale = transform.localScale;
		transform.localScale = Vector3.zero;
		StartCoroutine(ScaleInAnimation());
	}

	private IEnumerator ScaleInAnimation()
	{
		isScalingIn = true;
		float elapsed = 0f;

		while (elapsed < scaleInDuration)
		{
			elapsed += Time.deltaTime;
			float progress = scaleInCurve.Evaluate(elapsed / scaleInDuration);
			transform.localScale = targetScale * progress;
			yield return null;
		}

		transform.localScale = targetScale;
		isScalingIn = false;
	}

	public void StartScaleOut(bool destroy = true)
	{
		if (!isScalingOut)
		{
			StartCoroutine(ScaleOutAnimation(destroy));
		}
	}

	private IEnumerator ScaleOutAnimation(bool destroy)
	{
		isScalingOut = true;
		float elapsed = 0f;
		Vector3 startScale = transform.localScale;

		while (elapsed < scaleOutDuration)
		{
			elapsed += Time.deltaTime;
			float progress = scaleOutCurve.Evaluate(elapsed / scaleOutDuration);
			transform.localScale = startScale * progress;
			yield return null;
		}

		isScalingOut = false;
		if (destroy)
		{
			//Destroy(gameObject);
		}
	}

	public void StartMergeAnimation(Vector3 mergePoint, System.Action onComplete = null)
	{
		StartCoroutine(MergeAnimation(mergePoint, onComplete));
	}

	private IEnumerator MergeAnimation(Vector3 mergePoint, System.Action onComplete)
	{
		isScalingOut = true;
		float elapsed = 0f;
		Vector3 startScale = transform.localScale;
		Vector3 startPosition = transform.position;

		while (elapsed < moveToMergeDuration)
		{
			elapsed += Time.deltaTime;
			float progress = moveToMergeCurve.Evaluate(elapsed / moveToMergeDuration);

			// Move towards merge point
			transform.position = Vector3.Lerp(startPosition, mergePoint, progress);

			// Scale out simultaneously
			transform.localScale = startScale * (1 - progress);

			yield return null;
		}

		isScalingOut = false;
		onComplete?.Invoke();
	}

	private void ApplyMetadataScale()
	{
		if (metadata == null) return;

		boxCollider.size = Vector3.one;
		transform.localScale = Vector3.one * GameManager.Instance.SizeConverter(metadata.scale);
	}

	private void AdjustSpriteToCollider()
	{
		if (emojiRenderer == null || emojiRenderer.sprite == null || boxCollider == null)
		{
			Debug.LogError($"[LLement] Missing components in AdjustSpriteToCollider - Renderer: {emojiRenderer != null}, Sprite: {emojiRenderer?.sprite != null}, Collider: {boxCollider != null}");
			return;
		}

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

	/*private void OnCollisionEnter(Collision collision)
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
	}*/

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

		if (emojiRenderer == null)
		{
			emojiRenderer = GetComponentInChildren<SpriteRenderer>(true);
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
}