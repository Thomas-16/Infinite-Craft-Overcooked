using UnityEngine;
using System.Collections;
using System;
using Microsoft.Win32.SafeHandles;
using System.Security.Cryptography;

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
	private float lastFrameVelocity;

	public GameObject visuals;

	[SerializeField]
	private UIPanel namePanelPrefab;

	private void Awake()
	{
		originalScale = transform.localScale;
		SetupComponents();
		if (!string.IsNullOrEmpty(ElementName))
		{
			SetElementName(ElementName);
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
				//Debug.Log($"[LLement] Found existing emoji renderer: {emojiRenderer.gameObject.name}, Active: {emojiRenderer.gameObject.activeInHierarchy}");
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
		//Debug.Log($"[LLement] Created new emoji renderer: {emojiObj.name}, Active: {emojiObj.activeInHierarchy}");
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
			UpdateUIAlpha(fadeOutAlpha);
		}
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
    private void LateUpdate() {
		lastFrameVelocity = GetComponent<Rigidbody>().velocity.magnitude;
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

	public async void SetElementName(string elementName)
    {
        //Debug.Log($"[LLement] Setting element name to: {elementName}");
        ElementName = elementName;
        
        if (namePanel != null)
        {
            namePanel.SetText(elementName);
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
                            emojiRenderer.gameObject.SetActive(true);
                            emojiRenderer.sprite = emojiSprite;
                            //Debug.Log($"[LLement] Set sprite for {elementName}, Renderer active: {emojiRenderer.gameObject.activeInHierarchy}");
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
	public void SetElementNameAndSprite(string elementName, Sprite elementSprite) {
        ElementName = elementName;

        if (namePanel != null) {
            namePanel.SetText(elementName);
        }
        emojiRenderer.gameObject.SetActive(true);
        emojiRenderer.sprite = elementSprite;

        ApplyMetadataScale();
        AdjustSpriteToCollider();
    }

	private void ApplyMetadataScale()
	{
		if (metadata == null) return;

		boxCollider.size = Vector3.one;
		transform.localScale = Vector3.one * GameManager.Instance.SizeConverter(metadata.scale);
	}
    public float GetScale() => metadata.scale;
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
			//Debug.Log($"[LLement] Activated emoji renderer in AdjustSpriteToCollider");
		}

		Vector3 colliderSize = boxCollider.size;
		float smallestDimension = Mathf.Min(colliderSize.x, colliderSize.y);

		float targetSize = smallestDimension * spriteScale;

		Vector2 spriteSize = emojiRenderer.sprite.bounds.size;
		float maxSpriteSize = Mathf.Max(spriteSize.x, spriteSize.y);

		float scaleFactor = targetSize / maxSpriteSize;

		emojiRenderer.transform.localScale = Vector3.one * scaleFactor;
		//Debug.Log($"[LLement] Adjusted sprite scale to {scaleFactor}, Renderer active: {emojiRenderer.gameObject.activeInHierarchy}");
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
		GameObject mobHit = collision.gameObject;
		Zombie zombieHit = mobHit.GetComponentInParent<Zombie>();
		Animal animalHit = mobHit.GetComponentInParent<Animal>();

        if (zombieHit != null || animalHit != null) {
			//Debug.Log(lastFrameVelocity);
			float damage = GetScale() * lastFrameVelocity * .6f;
			Debug.Log("damage delt: " + damage);

            if (zombieHit != null) {
				zombieHit.Damage(damage);
			} else {
				animalHit.Damage(damage);
			}
			//mobHit.GetComponent<Rigidbody>().AddExplosionForce(damage * 50f, collision.contacts[0].point, .5f);
		}

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

	//private void OnValidate()
	//{
	//	if (boxCollider == null)
	//	{
	//		boxCollider = GetComponent<BoxCollider>();
	//		if (boxCollider == null)
	//		{
	//			boxCollider = gameObject.AddComponent<BoxCollider>();
	//		}
	//	}

	//	if (emojiRenderer == null)
	//	{
	//		emojiRenderer = GetComponentInChildren<SpriteRenderer>(true);
	//		if (emojiRenderer != null)
	//		{
	//			emojiRenderer.gameObject.SetActive(true);
	//			//Debug.Log($"[LLement] Found and activated emoji renderer in OnValidate");
	//		}
	//	}

	//	if (visuals == null || emojiRenderer == null)
	//	{
	//		SetupVisuals();
	//	}

	//	if (metadata != null)
	//	{
	//		ApplyMetadataScale();
	//		AdjustSpriteToCollider();
	//	}
	//}

	private void OnDestroy()
	{
		if (namePanel != null && UIManager.Instance != null)
		{
			UIManager.Instance.RemoveWorldPositionedPanel(transform);
		}
	}
	public SpriteRenderer GetEmojiRenderer() { return emojiRenderer; }

	public void UIPanelSetActive(bool active) {
		namePanel.ShouldBeActive = active;
    }
}