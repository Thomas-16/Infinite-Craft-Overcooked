using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
	[SerializeField] private float spriteSizeMultiplier = 0.3f; // 3/10ths of original size

	private ObjectMetadata metadata;
	private bool canTriggerMerge = false;
	private bool hasBeenHeld = false;

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

	public async void SetElementName(string elementName)
	{
		ElementName = elementName;

		metadata = await ObjectMetadataAPI.Instance.GetObjectMetadata(elementName);

		if (metadata != null)
		{
			// Convert emoji to sprite and apply it
			Sprite emojiSprite = await EmojiConverter.GetEmojiSprite(metadata.emoji);
			if (emojiSprite != null)
			{
				emojiRenderer.sprite = emojiSprite;
				AdjustSpriteScale();
			}

			// Apply scale
			transform.localScale = Vector3.one * (defaultScale/* * metadata.scale * 1f*/);

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
	}
}