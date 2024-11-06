using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using DG.Tweening;

public class GoalZone : MonoBehaviour
{
	[Header("Zone Settings")]
	[SerializeField] private BoxCollider triggerZone;
	[SerializeField] private ParticleSystem acceptEffect;
	[SerializeField] private ParticleSystem rejectEffect;
	[SerializeField] private float effectDuration = 1f;

	[Header("Goal Settings")]
	[SerializeField] private int requiredItems = 5;

	[Header("UI Settings")]
	[SerializeField] private UIPanel progressUIPrefab;

	[Header("Visual Feedback")]
	[SerializeField] private MeshRenderer cubeMeshRenderer;
	[SerializeField] private Color successColor = new Color(0, 1, 0, 0.5f); // Semi-transparent green
	[SerializeField] private Color rejectColor = new Color(1, 0, 0, 0.5f);  // Semi-transparent red
	[SerializeField] private float colorTransitionDuration = 0.3f;


	private int acceptedItems = 0;
	private bool isProcessing = false;
	private HashSet<LLement> processedElements = new HashSet<LLement>();
	private UIPanel progressPanel;
	private Material cubeMaterial;
	private Color originalColor;
	private Sequence currentColorSequence;

	private void Start()
	{
		SetupZone();
		UpdateProgressUI();
	}

	private void AnimateCubeColor(Color targetColor)
	{
		if (cubeMaterial == null) return;

		// Kill any ongoing color animation
		if (currentColorSequence != null)
		{
			currentColorSequence.Kill();
		}

		// Create new color transition sequence
		currentColorSequence = DOTween.Sequence();

		// Transition to target color and back
		currentColorSequence.Append(DOTween.To(() => cubeMaterial.color, x => cubeMaterial.color = x, targetColor, colorTransitionDuration)
			.SetEase(Ease.OutQuad));
		currentColorSequence.Append(DOTween.To(() => cubeMaterial.color, x => cubeMaterial.color = x, originalColor, colorTransitionDuration)
			.SetEase(Ease.InQuad));
	}

	private void SetupZone()
	{
		// Setup trigger zone
		if (triggerZone == null)
		{
			triggerZone = GetComponent<BoxCollider>();
			if (triggerZone != null)
			{
				triggerZone.isTrigger = true;
			}
		}

		// Create progress UI panel
		progressPanel = UIManager.Instance.CreateWorldPositionedPanel(transform, progressUIPrefab, Vector3.zero);

		// Initial setup of progress panel
		if (progressPanel != null)
		{
			progressPanel.SetText($"Offerings: 0/{requiredItems}");
			progressPanel.SetPanelColor(new Color(0, 0, 0, 0.7f));
			progressPanel.SetTextColor(Color.white);
		}

		if (cubeMeshRenderer != null)
		{
			// Create a material instance to avoid affecting other objects using the same material
			cubeMaterial = new Material(cubeMeshRenderer.material);
			cubeMeshRenderer.material = cubeMaterial;
			originalColor = cubeMaterial.color;
		}
		else
		{
			Debug.LogError("No MeshRenderer found for color feedback!");
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (isProcessing) return;

		LLement element = other.GetComponent<LLement>();
		if (element != null && !processedElements.Contains(element))
		{
			EvaluateElement(element);
		}
	}

	private async void EvaluateElement(LLement element)
	{
		Debug.Log("Evaluating element: " + element.ElementName);
		isProcessing = true;
		processedElements.Add(element);

		var (isAccepted, response) = await GodMessages.Instance.EvaluateOffering(element.ElementName);

		if (isAccepted)
		{
			HandleAcceptedElement(element);
		}
		else
		{
			HandleRejectedElement(element);
		}

		isProcessing = false;
	}

	private async void HandleAcceptedElement(LLement element)
	{
		acceptedItems++;
		UpdateProgressUI();
		AnimateCubeColor(successColor);

		if (acceptEffect != null)
		{
			ParticleSystem effect = Instantiate(acceptEffect, element.transform.position, Quaternion.identity);
			effect.Play();
			Destroy(effect.gameObject, effectDuration);
		}

		element.gameObject.SetActive(false);
		await Task.Delay((int)(effectDuration * 1000));
		Destroy(element.gameObject);

		CheckWinCondition();
	}

	private void HandleRejectedElement(LLement element)
	{
		AnimateCubeColor(rejectColor);

		if (rejectEffect != null)
		{
			ParticleSystem effect = Instantiate(rejectEffect, element.transform.position, Quaternion.identity);
			effect.Play();
			Destroy(effect.gameObject, effectDuration);
		}

		if (element.TryGetComponent<Rigidbody>(out Rigidbody rb))
		{
			Vector3 pushDirection = (element.transform.position - transform.position).normalized;
			rb.AddForce(pushDirection * 6f + Vector3.up * 3f, ForceMode.Impulse);
		}

		processedElements.Remove(element);
	}

	private void UpdateProgressUI()
	{
		if (progressPanel != null)
		{
			progressPanel.SetText($"Offerings: {acceptedItems}/{requiredItems}");
		}
	}

	private void CheckWinCondition()
	{
		if (acceptedItems >= requiredItems)
		{
			GodMessages.Instance.QueueMessage(
				"Your offerings have pleased me. You may go in peace!",
				Color.white,
				new Color(0.1f, 0.3f, 0.1f, 0.8f)
			);
			Debug.Log("[GoalZone] Win condition met!");
			// Add any additional win condition handling here
		}
	}

	private void OnDestroy()
	{
		// Clean up UI panels
		if (UIManager.Instance != null && progressPanel != null)
		{
			UIManager.Instance.RemoveWorldPositionedPanel(transform);
		}
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (triggerZone == null)
		{
			triggerZone = GetComponent<BoxCollider>();
		}
	}
#endif
}