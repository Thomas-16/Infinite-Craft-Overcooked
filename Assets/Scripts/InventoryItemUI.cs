using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour, IPointerDownHandler
{
    public static readonly int FULLSTACK_COUNT = 64;
    public static int HighestOrderLayer = 10;

    private PickupableObject pickupableObject;
    private Image itemImage;
    private Sprite itemSprite;
    private string itemName;
    private int itemCount;
    [SerializeField] private TextMeshProUGUI itemCountTxt;
    //[SerializeField] private TextMeshProUGUI itemNameTxt;

    private bool isPickedup;
    private bool canBePickedup = true;

    private void Awake() {
        itemImage = GetComponent<Image>();
    }
    private void Start() {
        itemImage.sprite = itemSprite;
    }
    private void Update() {
        if (isPickedup) {
            transform.position = Input.mousePosition;
        }
    }
    public void InitItemInfo(Sprite itemSprite, string itemName, PickupableObject pickupableObject) {
        itemCount = 1;
        this.itemSprite = itemSprite;
        this.itemName = itemName;
        this.pickupableObject = pickupableObject;

        UpdateItemInfo();
    }
    public void OnPointerDown(PointerEventData eventData) {
        if (!canBePickedup) return;

        if (!isPickedup) {
            Pickup();
        }
        else {
            Drop();
        }
    }
    public void Pickup() {
        isPickedup = !isPickedup;
        HighestOrderLayer++;
        GetComponent<Canvas>().sortingOrder = HighestOrderLayer;

        if(GetComponentInParent<InventorySlotUI>().IsHotbar) {
            PlayerInventorySystem.Instance.ClearHotbarSlot(GetComponentInParent<InventorySlotUI>().HotbarSlotIndex);
        }
    }
    public void Drop() {
        isPickedup = !isPickedup;

        InventorySlotUI slotDroppedOn = GetHoveredInventorySlotUI();
        CombiningSlotUI combiningSlotDroppedOn = GetHoveredCombiningSlotUI();

        if (slotDroppedOn == null && combiningSlotDroppedOn != null) {
            float minDistance = float.MaxValue;
            Vector2 mousePosition = Input.mousePosition;

            // Iterate through all InventorySlotUI elements
            InventorySlotUI[] allSlots = FindObjectsOfType<InventorySlotUI>();
            allSlots = allSlots.Where(slot => !PlayerInventorySystem.Instance.IsSlotResultSlot(slot)).ToArray();
            foreach (InventorySlotUI slot in allSlots) {
                if (slot == null) continue;

                // Get the position of the slot in screen space
                Vector2 slotPosition = slot.GetComponent<RectTransform>().position;

                // Calculate the distance between the mouse and the slot
                float distance = Vector2.Distance(mousePosition, slotPosition);

                // Update the closest slot if this one is closer
                if (distance < minDistance) {
                    minDistance = distance;
                    slotDroppedOn = slot;
                }
            }
            Debug.Log("dropping on closest one");
        }
        if(combiningSlotDroppedOn != null) {
            if (combiningSlotDroppedOn.HasItem() && combiningSlotDroppedOn.GetItem() != this) {
                combiningSlotDroppedOn.GetItem().Pickup();
            }

            transform.SetParent(combiningSlotDroppedOn.transform);
            transform.position = combiningSlotDroppedOn.transform.position;
        } else {
            if (slotDroppedOn.HasItem() && slotDroppedOn.GetItem() != this) {
                slotDroppedOn.GetItem().Pickup();
            }

            transform.SetParent(slotDroppedOn.transform);
            transform.position = slotDroppedOn.transform.position;

            if (slotDroppedOn.IsHotbar) {
                PlayerInventorySystem.Instance.SetHotbarSlot(slotDroppedOn.HotbarSlotIndex, GetPhysicalItem(), itemName, itemSprite, itemCount);
            }
        }

    }
    public void UpdateItemInfo() {
        itemCountTxt.text = itemCount.ToString();
    }
    public PickupableObject GetPhysicalItem() { return pickupableObject; }
    public void AddItemToStack() {
        itemCount++;
        UpdateItemInfo();
    }
    public void RemoveItemFromStack() {
        itemCount--;
        UpdateItemInfo();
    }
    public string GetItemName() { return itemName;  }
    public int GetStackCount() { return itemCount; }

    private InventorySlotUI GetHoveredInventorySlotUI() {
        // Create a PointerEventData object with the current mouse position
        PointerEventData pointerData = new PointerEventData(EventSystem.current) {
            position = Input.mousePosition
        };

        // List to hold the raycast results
        List<RaycastResult> raycastResults = new List<RaycastResult>();

        // Perform the UI raycast
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        // Loop through the raycast results
        foreach (RaycastResult result in raycastResults) {
            // Check if the GameObject has an InventorySlotUI component
            InventorySlotUI slotUI = result.gameObject.GetComponent<InventorySlotUI>();
            if (slotUI != null && !PlayerInventorySystem.Instance.IsSlotResultSlot(slotUI)) {
                return slotUI; // Return the first InventorySlotUI found
            }
        }

        // Return null if no InventorySlotUI is found
        return null;
    }
    private CombiningSlotUI GetHoveredCombiningSlotUI() {
        // Create a PointerEventData object with the current mouse position
        PointerEventData pointerData = new PointerEventData(EventSystem.current) {
            position = Input.mousePosition
        };

        // List to hold the raycast results
        List<RaycastResult> raycastResults = new List<RaycastResult>();

        // Perform the UI raycast
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        // Loop through the raycast results
        foreach (RaycastResult result in raycastResults) {
            // Check if the GameObject has an InventorySlotUI component
            CombiningSlotUI slotUI = result.gameObject.GetComponent<CombiningSlotUI>();
            if (slotUI != null) {
                return slotUI; // Return the first InventorySlotUI found
            }
        }

        // Return null if no InventorySlotUI is found
        return null;
    }
    public bool IsPickedUp() {
        return isPickedup;
    }
    public void SetCanBePickedUp(bool b) {
        canBePickedup = b;
    }

}
