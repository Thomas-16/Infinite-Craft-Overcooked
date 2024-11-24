using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombiningSlotUI : MonoBehaviour
{
    [SerializeField] private Color unselectedColour;
    [SerializeField] private Color selectedColour;

    private bool isSelected;
    private Image bgImage;

    private void Awake() {
        bgImage = GetComponent<Image>();
    }
    private void Update() {
        Vector2 rectPosition = GetComponent<RectTransform>().position; // Center position of the UI element in screen space
        Vector2 rectSize = GetComponent<RectTransform>().sizeDelta;   // Size of the UI element in local space

        // Calculate the boundaries of the RectTransform
        float left = rectPosition.x - rectSize.x * 0.5f;
        float right = rectPosition.x + rectSize.x * 0.5f;
        float bottom = rectPosition.y - rectSize.y * 0.5f;
        float top = rectPosition.y + rectSize.y * 0.5f;

        // Check if the mouse position is within the bounds
        isSelected = Input.mousePosition.x >= left && Input.mousePosition.x <= right &&
               Input.mousePosition.y >= bottom && Input.mousePosition.y <= top;

        bgImage.color = isSelected ? selectedColour : unselectedColour;
    }
    public bool HasItem() {
        return GetComponentInChildren<InventoryItemUI>() != null;
    }
    public InventoryItemUI GetItem() {
        return GetComponentInChildren<InventoryItemUI>();
    }
    public void DestroyItem() {
        Destroy(GetItem().gameObject);
    }

}
