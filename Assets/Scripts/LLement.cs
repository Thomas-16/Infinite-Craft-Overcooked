using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LLement : PickupableObject
{
	[field: SerializeField]
	public string ElementName { get; private set; }

	[Header("Merge Settings")]
	[SerializeField] private float mergeForceThreshold = 5f;
	[SerializeField] private float mergeCheckDelay = 0.1f;

	private bool canTriggerMerge = false;  // Can this element initiate a merge?
	private bool hasBeenHeld = false;      // Has this element ever been held by a player?

	[SerializeField] private TextMeshPro text1;
	[SerializeField] private TextMeshPro text2;
	[SerializeField] private TextMeshPro text3;
	[SerializeField] private TextMeshPro text4;
	[SerializeField] private TextMeshPro text5;
	[SerializeField] private TextMeshPro text6;

	private void Awake()
	{
		SetElementName(ElementName);
	}

	public void SetElementName(string elementName)
	{
		ElementName = elementName;
		text1.text = ElementName;
		text2.text = ElementName;
		text3.text = ElementName;
		text4.text = ElementName;
		text5.text = ElementName;
		text6.text = ElementName;
	}

	public override void Pickup(Player player)
	{
		base.Pickup(player);
		hasBeenHeld = true;      // Mark that this element has been held
		canTriggerMerge = false; // Disable merging while held
	}

	public override void Drop(Player player)
	{
		base.Drop(player);
		StartCoroutine(EnableMergeAfterDelay());
	}

	private IEnumerator EnableMergeAfterDelay()
	{
		yield return new WaitForSeconds(mergeCheckDelay);
		canTriggerMerge = true;  // Enable this element to trigger merges
	}

	private void OnCollisionEnter(Collision collision)
	{
		// Only proceed if this element can trigger merges (was previously held and dropped)
		if (!canTriggerMerge || !hasBeenHeld) return;

		LLement otherElement = collision.gameObject.GetComponent<LLement>();
		if (otherElement != null) // Notice we don't check if the other element canTriggerMerge
		{
			float collisionForce = collision.impulse.magnitude;

			if (collisionForce >= mergeForceThreshold)
			{
				// Disable further merges for both elements
				canTriggerMerge = false;
				otherElement.canTriggerMerge = false;

				// Initiate the merge
				GameManager.Instance.MergeElements(this, otherElement);
			}
		}
	}

	// Optional: Method to check if this element has ever been handled by a player
	public bool HasBeenPlayerHandled()
	{
		return hasBeenHeld;
	}
}