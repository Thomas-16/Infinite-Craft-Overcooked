using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

public class GoalZone : MonoBehaviour
{
	[Header("Zone Settings")]
	[SerializeField] private BoxCollider triggerZone;
	[SerializeField] private ParticleSystem acceptEffect;
	[SerializeField] private ParticleSystem rejectEffect;
	[SerializeField] private float effectDuration = 1f;

	[Header("Goal Settings")]
	[SerializeField] private int requiredItems = 5;

	private string alienTastes;
	private int acceptedItems = 0;
	private bool isProcessing = false;
	private HashSet<LLement> processedElements = new HashSet<LLement>();
	private UIPanel mainPanel;
	private UIPanel progressPanel;

	[SerializeField] private UIPanel progressUIPrefab;
	[SerializeField] private UIPanel dialogueUIPrefab;

	[SerializeField] private Transform dialogueReference;

	private async void Start()
	{
		SetupZone();
		await GenerateAlienTastes();
		UpdateProgressUI();
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

		// Create UI panels
		mainPanel = UIManager.Instance.CreateWorldPositionedPanel(dialogueReference, dialogueUIPrefab, Vector3.zero);
		progressPanel = UIManager.Instance.CreateWorldPositionedPanel(transform, progressUIPrefab, Vector3.zero);

		// Initial setup of panels
		if (mainPanel != null)
		{
			mainPanel.SetText("Awaiting alien preferences...");
		}

		if (progressPanel != null)
		{
			progressPanel.SetText("Satisfied: 0/" + requiredItems);
			// Optionally set different colors/styles for progress panel
			progressPanel.SetPanelColor(new Color(0, 0, 0, 0.7f));
			progressPanel.SetTextColor(Color.white);
		}
	}

	private async Task GenerateAlienTastes()
	{
		string prompt = "You are an alien food critic with very specific tastes. Describe your preferences for Earth objects in 2-3 sentences. " +
					   "Keep the sentences short. Include a line break after every phrase. " +
					   "Include specific characteristics you love and hate. Be creative but consistent. " +
					   "For example: 'I adore things made of wood \n " +
					   "that remind me of my marshy homeworld, \n especially if they're squishy. \n " +
					   "I despise anything metallic or artificial.'";

		alienTastes = await ChatGPTClient.Instance.SendChatRequest(prompt);

		if (mainPanel != null)
		{
			mainPanel.SetText("Alien: " + alienTastes);
		}

		Debug.Log($"[GoalZone] Alien's tastes: {alienTastes}");
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
		Debug.Log("evaluate element: " + element.ElementName);
		isProcessing = true;
		processedElements.Add(element);

		string prompt = $"Based on your stated preferences: '{alienTastes}'\n" +
					   $"Would you eat a {element.ElementName}? Consider the object's properties and your established tastes.\n" +
					   $"Reply with ONLY 'YES' or 'NO' followed by a brief one-sentence explanation.";

		string response = await ChatGPTClient.Instance.SendChatRequest(prompt);
		bool isAccepted = response.Trim().StartsWith("YES", System.StringComparison.OrdinalIgnoreCase);

		if (isAccepted)
		{
			HandleAcceptedElement(element, response);
		}
		else
		{
			HandleRejectedElement(element, response);
		}

		isProcessing = false;
	}

	private async void HandleAcceptedElement(LLement element, string response)
	{
		acceptedItems++;
		UpdateProgressUI();

		if (acceptEffect != null)
		{
			ParticleSystem effect = Instantiate(acceptEffect, element.transform.position, Quaternion.identity);
			effect.Play();
			Destroy(effect.gameObject, effectDuration);
		}

		element.gameObject.SetActive(false);
		await Task.Delay((int)(effectDuration * 1000));
		Destroy(element.gameObject);

		if (mainPanel != null)
		{
			mainPanel.SetText("Alien: " + response);
		}

		Debug.Log("HandleAcceptedElement: " + element.ElementName + ", response: " + response);

		CheckWinCondition();
	}

	private async void HandleRejectedElement(LLement element, string response)
	{
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

		if (mainPanel != null)
		{
			mainPanel.SetText("Alien: " + response);
		}

		Debug.Log("HandleRejectedElement: " + element.ElementName + ", response: " + response);

		processedElements.Remove(element);
	}

	private void UpdateProgressUI()
	{
		if (progressPanel != null)
		{
			progressPanel.SetText($"Satisfied: {acceptedItems}/{requiredItems}");
		}
	}

	private void CheckWinCondition()
	{
		if (acceptedItems >= requiredItems)
		{
			if (mainPanel != null)
			{
				mainPanel.SetText("Alien: Thank you! I am satisfied!");
			}
			Debug.Log("[GoalZone] Win condition met!");
			// Add any additional win condition handling here
		}
	}

	private void OnDestroy()
	{
		// Clean up UI panels
		if (UIManager.Instance != null)
		{
			if (mainPanel != null)
			{
				UIManager.Instance.RemoveWorldPositionedPanel(transform);
			}
			if (progressPanel != null)
			{
				UIManager.Instance.RemoveWorldPositionedPanel(transform);
			}
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