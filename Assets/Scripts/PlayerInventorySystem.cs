using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventorySystem : MonoBehaviour
{
    private Player player;
    private int selectedIndex;

    [SerializeField] private HotbarSlotUI[] hotbarItemsUI;
    private PickupableObject[] hotbarItems;

    private void Awake() {
        player = GetComponent<Player>();

        hotbarItems = new PickupableObject[hotbarItemsUI.Length];
    }
    private void Start() {
        InputManager.Instance.OnNumberKeyPressed += OnNumberKeyPressedHandler;
        InputManager.Instance.OnScrollItemSwitch += OnScrollItemSwitchHandler;
    }

    public void AddItem(PickupableObject pickedUpObj) {
        int i = 0;
        int emptySlotIndex = -1;
        int stackableSlotIndex = -1;

        while (i < hotbarItems.Length) {
            // Check if this slot is empty (no item)
            if (!hotbarItemsUI[i].HasItem && emptySlotIndex == -1) {
                emptySlotIndex = i; // Remember the first empty slot
            }

            // Check if this slot has the same item and can be stacked
            if (hotbarItemsUI[i].ItemName == (pickedUpObj as LLement).ElementName && hotbarItemsUI[i].StackCount < HotbarSlotUI.FULLSTACK_COUNT) {
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


        hotbarItems[i] = pickedUpObj;
        if(!stackingIntoStack) {
            hotbarItemsUI[i].SetItem(((LLement) pickedUpObj).ElementName, ((LLement) pickedUpObj).GetEmojiRenderer().sprite, 1);
            pickedUpObj.Pickup(player);
            Debug.Log("picked up item not into stack");
        } else {
            hotbarItemsUI[i].AddItemToStack();
            Destroy(pickedUpObj.gameObject);
            Debug.Log("picked up item into stack");
        }
        
        if(pickedUpObj is LLement) {
            ((LLement)pickedUpObj).UIPanelSetActive(false);
        }

        UpdateHoldingItem();
    }
    public void DropItem() {
        if (hotbarItemsUI[selectedIndex].StackCount == 1) {
            hotbarItems[selectedIndex].Drop(player);
            
            hotbarItemsUI[selectedIndex].RemoveItemFromStack();

            if (hotbarItems[selectedIndex] is LLement) {
                ((LLement)hotbarItems[selectedIndex]).UIPanelSetActive(false);
            }
            hotbarItems[selectedIndex] = null;

            Debug.Log("dropping not from stack");

        } else if (hotbarItemsUI[selectedIndex].StackCount > 1) {
            PickupableObject dupedItem = Instantiate(hotbarItems[selectedIndex].gameObject, hotbarItems[selectedIndex].gameObject.transform.position, hotbarItems[selectedIndex].gameObject.transform.rotation, hotbarItems[selectedIndex].gameObject.transform.parent).GetComponent<PickupableObject>();
            
            dupedItem.Drop(player);
            hotbarItemsUI[selectedIndex].RemoveItemFromStack();

            if (dupedItem is LLement) {
                ((LLement)dupedItem).UIPanelSetActive(false);
            }

            Debug.Log("dropping from stack");
        }
        
    }
    public void ThrowItem() {

    }
    public bool IsInventoryFull() {
        return hotbarItems[8] != null;
    }
    private void UpdateHoldingItem() {
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

        if (index >= hotbarItemsUI.Length) {
            index = 0;
        }
        else if (index <= -1) {
            index = hotbarItemsUI.Length - 1;
        }
        SelectItem(index);
    }
    private void OnNumberKeyPressedHandler(int selected) {
        if (selected - 1 < 0 || selected - 1 >= hotbarItemsUI.Length) {
            return;
        }
        SelectItem(selected - 1);
    }

    private void SelectItem(int index) {
        hotbarItemsUI[selectedIndex].IsSelected = false;

        selectedIndex = index;
        hotbarItemsUI[selectedIndex].IsSelected = true;

        UpdateHoldingItem();
    }
    public PickupableObject GetCurrentHoldingItem() { return hotbarItems[selectedIndex]; }
}
