using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using DG.Tweening;

public class StoryGoalZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private BoxCollider triggerZone;
    [SerializeField] private ParticleSystem acceptEffect;
    [SerializeField] private ParticleSystem rejectEffect;
    [SerializeField] private float effectDuration = 1f;

    [Header("UI Reference")]
    [SerializeField] private UIPanel zonePanel;

    [Header("Visual Feedback")]
    [SerializeField] private MeshRenderer cubeMeshRenderer;
    [SerializeField] private Color successColor = new Color(0, 1, 0, 0.5f); // Semi-transparent green
    [SerializeField] private Color rejectColor = new Color(1, 0, 0, 0.5f);  // Semi-transparent red
    [SerializeField] private float colorTransitionDuration = 0.3f;

    private bool isProcessing = false;
    private HashSet<LLement> processedElements = new HashSet<LLement>();
    private Material cubeMaterial;
    private Color originalColor;
    private Sequence currentColorSequence;

    private string zoneTextLabel = "";

    private void Start()
    {
        SetupZone();
        UpdatePanelText();
    }

    private void Update() {
        if (zoneTextLabel == "") {
            UpdatePanelText();
        }
    }

    private void SetupZone()
    {
        if (triggerZone == null)
        {
            triggerZone = GetComponent<BoxCollider>();
            if (triggerZone != null)
            {
                triggerZone.isTrigger = true;
            }
        }

        if (zonePanel == null)
        {
            Debug.LogError("No UI Panel assigned to StoryGoalZone!");
        }

        if (cubeMeshRenderer == null)
        {
            cubeMeshRenderer = GetComponent<MeshRenderer>();
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

    private void UpdatePanelText()
    {
        if (zonePanel == null) return;

        StorySegment currentSegment = StoryMessages.Instance.GetCurrentSegment();
        if (currentSegment != null)
        {
            zoneTextLabel = $"Place\n{char.ToUpper(currentSegment.requiredWordType[0])}{currentSegment.requiredWordType.Substring(1)} word\nhere";
            zonePanel.SetText(zoneTextLabel);
            zonePanel.SetPanelColor(new Color(0, 0, 0, 0.7f));
            zonePanel.SetTextColor(Color.white);
        }
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
        Debug.Log("Evaluating word: " + element.ElementName);
        isProcessing = true;
        processedElements.Add(element);

        bool isAccepted = await StoryMessages.Instance.EvaluateWord(element.ElementName);

        if (isAccepted)
        {
            HandleAcceptedElement(element);
            UpdatePanelText(); // Update text for next word type
        }
        else
        {
            HandleRejectedElement(element);
        }

        isProcessing = false;
    }

    private async void HandleAcceptedElement(LLement element)
    {
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

    private void OnDestroy()
    {
        if (cubeMaterial != null)
        {
            Destroy(cubeMaterial);
        }
        
        if (currentColorSequence != null)
        {
            currentColorSequence.Kill();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (triggerZone == null)
        {
            triggerZone = GetComponent<BoxCollider>();
        }
        
        if (cubeMeshRenderer == null)
        {
            cubeMeshRenderer = GetComponent<MeshRenderer>();
        }
    }
#endif
}