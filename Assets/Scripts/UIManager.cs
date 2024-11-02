using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
	public static UIManager Instance { get; private set; }

	[Header("UI Prefabs")]
	[SerializeField] private UIPanel goalZoneUIPrefab;
	// Add more UI panel prefabs as needed

	[Header("UI Settings")]
	[SerializeField] private float edgePadding = 20f;

	private Dictionary<Transform, (UIPanel panel, Vector3 offset)> worldToUIPanels = new Dictionary<Transform, (UIPanel, Vector3)>();
	private Canvas mainCanvas;
	private Camera mainCamera;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		mainCanvas = GetComponent<Canvas>();
		mainCamera = Camera.main;
	}

	private void LateUpdate()
	{
		UpdateWorldPositionedPanels();
	}

	public UIPanel CreateWorldPositionedPanel(Transform worldSpaceTarget, UIPanel prefab, Vector3 offset)
	{
		if (prefab == null)
		{
			Debug.LogError("UI Panel prefab not assigned!");
			return null;
		}

		// Create panel from prefab
		UIPanel panel = Instantiate(prefab, mainCanvas.transform);

		// Setup anchors and pivot
		panel.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		panel.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		panel.RectTransform.pivot = new Vector2(0.5f, 0.5f);

		// Add to tracking dictionary with offset
		worldToUIPanels[worldSpaceTarget] = (panel, offset);

		Debug.Log("!!!! panel created: " + panel.name);
		return panel;
	}

	public void RemoveWorldPositionedPanel(Transform worldSpaceTarget)
	{
		if (worldToUIPanels.TryGetValue(worldSpaceTarget, out var panelData))
		{
			if (panelData.panel != null)
			{
				Destroy(panelData.panel.gameObject);
			}
			worldToUIPanels.Remove(worldSpaceTarget);
		}
	}

	private void UpdateWorldPositionedPanels()
	{
		if (mainCamera == null) return;

		List<Transform> invalidTargets = new List<Transform>();

		foreach (var kvp in worldToUIPanels)
		{
			Transform worldTarget = kvp.Key;
			var (panel, offset) = kvp.Value;

			if (worldTarget == null || panel == null)
			{
				invalidTargets.Add(worldTarget);
				continue;
			}

			UpdatePanelPosition(worldTarget, panel, offset);
		}

		// Cleanup invalid entries
		foreach (var target in invalidTargets)
		{
			RemoveWorldPositionedPanel(target);
		}
	}

	private void UpdatePanelPosition(Transform worldTarget, UIPanel panel, Vector3 offset)
	{
		// Get world position with offset
		Vector3 targetWorldPosition = worldTarget.position + offset;

		// Convert to screen position
		Vector3 screenPoint = mainCamera.WorldToScreenPoint(targetWorldPosition);

		// Check if behind camera
		if (screenPoint.z < 0)
		{
			panel.gameObject.SetActive(false);
			return;
		}

		panel.gameObject.SetActive(true);

		// Convert to canvas position
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			mainCanvas.GetComponent<RectTransform>(),
			screenPoint,
			mainCanvas.worldCamera,
			out Vector2 canvasPosition
		);

		// Apply position
		panel.RectTransform.anchoredPosition = canvasPosition;

		// Keep within screen bounds
		//ClampToScreen(panel.RectTransform);
	}

	private void ClampToScreen(RectTransform rectTransform)
	{
		Rect screenRect = mainCanvas.GetComponent<RectTransform>().rect;
		Vector2 currentPos = rectTransform.anchoredPosition;
		Vector2 size = rectTransform.sizeDelta;

		float minX = -screenRect.width / 2 + size.x / 2 + edgePadding;
		float maxX = screenRect.width / 2 - size.x / 2 + edgePadding;
		float minY = -screenRect.height / 2 + size.y / 2 + edgePadding;
		float maxY = screenRect.height / 2 - size.y / 2 + edgePadding;

		currentPos.x = Mathf.Clamp(currentPos.x, minX, maxX);
		currentPos.y = Mathf.Clamp(currentPos.y, minY, maxY);

		rectTransform.anchoredPosition = currentPos;
	}
}