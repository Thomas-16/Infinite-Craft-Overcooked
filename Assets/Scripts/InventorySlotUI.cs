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

    [field: SerializeField] public int StackCount { get; private set; }

    private bool isSelected;
    private Image bgImage;
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemCountTxt;


    private void Awake() {
        bgImage = GetComponent<Image>();
    }
    
    private void Update() {
        bgImage.color = isSelected ? selectedColour : unselectedColour;
    }
    public bool HasItem() {
        // TODO: return if item found as transform child
        return false;
    }
    public void AddItemToStack() {
        StackCount++;
    }
    public void RemoveItemFromStack() {
        StackCount--;
    }


    public void OnPointerEnter(PointerEventData eventData) {
        isSelected = true;
    }

    public void OnPointerExit(PointerEventData eventData) {
        isSelected = false;
    }
}
