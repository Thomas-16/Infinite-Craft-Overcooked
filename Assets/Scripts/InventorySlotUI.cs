using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static readonly int FULLSTACK_COUNT = 64;

    [SerializeField] private Color unselectedColour;
    [SerializeField] private Color selectedColour;

    private bool isSelected;
    private Image bgImage;

    private void Awake() {
        bgImage = GetComponent<Image>();
    }
    
    private void Update() {
        bgImage.color = isSelected ? selectedColour : unselectedColour;
    }
    public bool HasItem() {
        return GetComponentInChildren<InventoryItemUI>() != null;
    }
    public InventoryItemUI GetItem() {
        return GetComponentInChildren<InventoryItemUI>();
    }
    public void DestroyItem() {
        Destroy(GetComponentInChildren<InventoryItemUI>().gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        isSelected = true;
    }

    public void OnPointerExit(PointerEventData eventData) {
        isSelected = false;
    }
}
