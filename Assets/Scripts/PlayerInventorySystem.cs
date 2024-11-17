using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Burst;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventorySystem : MonoBehaviour
{
    private Player player;
    private int selectedIndex;
    private bool isInventoryOpen = false;

    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private GameObject inventoryItemUIPrefab;
    [SerializeField] private HotbarSlotUI[] hotbarItemSlotsUI;
    [SerializeField] private InventorySlotUI[] hotbarInventoryItemSlotsUI;
    [SerializeField] private InventorySlotUI[] inventoryItemSlotsUI;
    
    private PickupableObject[] hotbarItems;

    private void Awake() {
        player = GetComponent<Player>();

        hotbarItems = new PickupableObject[hotbarItemSlotsUI.Length];
    }
    private void Start() {
        InputManager.Instance.OnNumberKeyPressed += OnNumberKeyPressedHandler;
        InputManager.Instance.OnScrollItemSwitch += OnScrollItemSwitchHandler;
        InputManager.Instance.inputActions.Player.Inventory.performed += OnOpenCloseInventoryHandler;
    }

    public void AddItem(PickupableObject pickedUpObj) {
        int i = 0;
        int emptySlotIndex = -1;
        int stackableSlotIndex = -1;

        while (i < hotbarItems.Length) {
            // Check if this slot is empty (no item)
            if (!hotbarItemSlotsUI[i].HasItem && emptySlotIndex == -1) {
                emptySlotIndex = i; // Remember the first empty slot
            }

            // Check if this slot has the same item and can be stacked
            if (hotbarItemSlotsUI[i].ItemName == (pickedUpObj as LLement).ElementName && hotbarItemSlotsUI[i].StackCount < HotbarSlotUI.FULLSTACK_COUNT) {
                stackableSlotIndex = i; // Remember the first slot that can stack the item
                break; // Prioritize stacking into an existing stack
            }

            i++;
        }

        bool stackingIntoStack;
        // Decide the slot to use
        if (stackableSlotIndex != -1) {
            // Stack into the existing stack
            i = stackableSlotIndex;
            stackingIntoStack = true;
        }
        else if (emptySlotIndex != -1) {
            // Use the first empty slot
            i = emptySlotIndex;
            stackingIntoStack = false;
        }
        else {
            // No empty slots or available stacks, return (inventory is full)
            return;
        }

        if(!stackingIntoStack) {
            string itemName = ((LLement)pickedUpObj).ElementName;
            Sprite itemSprite = ((LLement)pickedUpObj).GetEmojiRenderer().sprite;
            InventoryItemUI newInventoryItem = Instantiate(inventoryItemUIPrefab, hotbarInventoryItemSlotsUI[i].transform.position, Quaternion.identity, hotbarInventoryItemSlotsUI[i].transform).GetComponent<InventoryItemUI>();
            newInventoryItem.InitItemInfo(itemSprite, itemName, pickedUpObj);

            hotbarItemSlotsUI[i].InitItemInfo(itemName, itemSprite, 1);
            hotbarItems[i] = pickedUpObj;
            pickedUpObj.Pickup(player);

            Debug.Log("picked up item not into stack");
        } else {
            hotbarItemSlotsUI[i].AddItemToStack();
            hotbarInventoryItemSlotsUI[i].GetItem().AddItemToStack();
            Destroy(pickedUpObj.gameObject);

            Debug.Log("picked up item into stack");
        }
        
        if(pickedUpObj is LLement) {
            ((LLement)pickedUpObj).UIPanelSetActive(false);
        }

        UpdateHoldingItemGameObject();
    }
    public void DropItem() {
        if (hotbarItemSlotsUI[selectedIndex].StackCount == 1) {
            hotbarItems[selectedIndex].Drop(player);
            
            hotbarItemSlotsUI[selectedIndex].RemoveItemFromStack();
            hotbarInventoryItemSlotsUI[selectedIndex].DestroyItem();

            if (hotbarItems[selectedIndex] is LLement) {
                ((LLement)hotbarItems[selectedIndex]).UIPanelSetActive(true);
            }
            hotbarItems[selectedIndex].SetPickupTimer(4f);
            hotbarItems[selectedIndex] = null;

            Debug.Log("dropping not from stack");

        } else if (hotbarItemSlotsUI[selectedIndex].StackCount > 1) {
            PickupableObject dupedItem = Instantiate(hotbarItems[selectedIndex].gameObject, hotbarItems[selectedIndex].gameObject.transform.position, hotbarItems[selectedIndex].gameObject.transform.rotation, hotbarItems[selectedIndex].gameObject.transform.parent).GetComponent<PickupableObject>();

            dupedItem.PickedupByPlayer = player;
            dupedItem.Drop(player);
            dupedItem.SetPickupTimer(4f);
            hotbarItemSlotsUI[selectedIndex].RemoveItemFromStack();
            hotbarInventoryItemSlotsUI[selectedIndex].GetItem().RemoveItemFromStack();

            if (dupedItem is LLement) {
                ((LLement)dupedItem).UIPanelSetActive(true);
            }

            Debug.Log("dropping from stack");
        }
        
    }
    public void ThrowItem(float throwChargeStartTime, float maxChargeTime, float minThrowForce, float maxThrowForce, float throwUpwardAngle) {
        float chargeTime = Mathf.Min(Time.time - throwChargeStartTime, maxChargeTime);
        float chargePercent = chargeTime / maxChargeTime;
        float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargePercent);

        Vector3 throwDirection = transform.forward + (Vector3.up * throwUpwardAngle);
        throwDirection.Normalize();

        if (hotbarItemSlotsUI[selectedIndex].StackCount == 1) {
            hotbarItems[selectedIndex].Drop(player);

            hotbarItemSlotsUI[selectedIndex].RemoveItemFromStack();
            hotbarInventoryItemSlotsUI[selectedIndex].DestroyItem();

            if (hotbarItems[selectedIndex] is LLement) {
                ((LLement)hotbarItems[selectedIndex]).UIPanelSetActive(true);
            }
            hotbarItems[selectedIndex].SetPickupTimer(4f);
            hotbarItems[selectedIndex].GetComponent<Rigidbody>().AddForce(throwDirection * throwForce);
            hotbarItems[selectedIndex] = null;

            Debug.Log("throwing not from stack");

        }
        else if (hotbarItemSlotsUI[selectedIndex].StackCount > 1) {
            PickupableObject dupedItem = Instantiate(hotbarItems[selectedIndex].gameObject, hotbarItems[selectedIndex].gameObject.transform.position, hotbarItems[selectedIndex].gameObject.transform.rotation, hotbarItems[selectedIndex].gameObject.transform.parent).GetComponent<PickupableObject>();

            dupedItem.PickedupByPlayer = player;
            dupedItem.Drop(player);
            dupedItem.SetPickupTimer(4f);
            hotbarItemSlotsUI[selectedIndex].RemoveItemFromStack();
            hotbarInventoryItemSlotsUI[selectedIndex].GetItem().RemoveItemFromStack();

            if (dupedItem is LLement) {
                ((LLement)dupedItem).UIPanelSetActive(true);
            }
            dupedItem.GetComponent<Rigidbody>().AddForce(throwDirection * throwForce);

            Debug.Log("throwing from stack");
        }
    }
    private void OnOpenCloseInventoryHandler(InputAction.CallbackContext context) {
        isInventoryOpen = !isInventoryOpen;
        inventoryUI.SetActive(isInventoryOpen);
    }
    // TODO: REFACTOR THIS
    public bool IsInventoryFull() {
        return hotbarItems[8] != null;
    }
    private void UpdateHoldingItemGameObject() {
        foreach(PickupableObject item in hotbarItems) { 
            if(item == null) continue;

            item.gameObject.SetActive(false);
        }
        if(hotbarItems[selectedIndex] != null) {
            hotbarItems[selectedIndex].gameObject.SetActive(true);
        }
    }

    private void OnScrollItemSwitchHandler(int scrollDelta) {
        int index = selectedIndex + scrollDelta;

        if (index >= hotbarItemSlotsUI.Length) {
            index = 0;
        }
        else if (index <= -1) {
            index = hotbarItemSlotsUI.Length - 1;
        }
        SelectItem(index);
    }
    private void OnNumberKeyPressedHandler(int selected) {
        if (selected - 1 < 0 || selected - 1 >= hotbarItemSlotsUI.Length) {
            return;
        }
        SelectItem(selected - 1);
    }

    private void SelectItem(int index) {
        hotbarItemSlotsUI[selectedIndex].IsSelected = false;

        selectedIndex = index;
        hotbarItemSlotsUI[selectedIndex].IsSelected = true;

        UpdateHoldingItemGameObject();
    }
    public bool IsInventoryOpen() { return isInventoryOpen; }
    public PickupableObject GetCurrentHoldingItem() { return hotbarItems[selectedIndex]; }
}
