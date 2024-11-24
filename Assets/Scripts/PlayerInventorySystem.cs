using System;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class PlayerInventorySystem : MonoBehaviour
{
    public static PlayerInventorySystem Instance { get; private set; }

    private Player player;
    private int selectedIndex;
    private bool isInventoryOpen = false;
    private bool isCombining;

    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private GameObject inventoryItemUIPrefab;
    [SerializeField] private HotbarSlotUI[] hotbarItemSlotsUI;
    [SerializeField] private InventorySlotUI[] hotbarInventoryItemSlotsUI;
    [SerializeField] private InventorySlotUI[] inventoryItemSlotsUI;
    [SerializeField] private CombiningSlotUI combiningSlot1;
    [SerializeField] private CombiningSlotUI combiningSlot2;
    [SerializeField] private InventorySlotUI resultSlot;
    [SerializeField] private Button combineButton;
    private PickupableObject[] hotbarPhysicalItems;

    private void Awake() {
        player = GetComponent<Player>();
        Instance = this;
        hotbarPhysicalItems = new PickupableObject[hotbarItemSlotsUI.Length];

        for(int i = 0; i < hotbarInventoryItemSlotsUI.Length; i++) {
            hotbarInventoryItemSlotsUI[i].IsHotbar = true;
            hotbarInventoryItemSlotsUI[i].HotbarSlotIndex = i;
        }
    }
    private void Start() {
        InputManager.Instance.OnNumberKeyPressed += OnNumberKeyPressedHandler;
        InputManager.Instance.OnScrollItemSwitch += OnScrollItemSwitchHandler;
        InputManager.Instance.inputActions.Player.Inventory.performed += OnOpenCloseInventoryHandler;
        combineButton.onClick.AddListener(CombineButtonClickedHandler);
    }

    public void AddItem(PickupableObject pickedUpObj) {
        int i = 0;
        int emptyHotbarSlotIndex = -1;
        int stackableHotbarSlotIndex = -1;
        int emptyInventorySlotIndex = -1;
        int stackableInventorySlotIndex = -1;
        bool puttingInInventory;
        bool stackingIntoStack;

        // Pass 1: Check for stackable slots
        // 1. Check the hotbar for stackable slots
        while (i < hotbarItemSlotsUI.Length) {
            // Check if this slot has the same item and can be stacked
            if (hotbarItemSlotsUI[i].ItemName == (pickedUpObj as LLement).ElementName &&
                hotbarItemSlotsUI[i].StackCount < HotbarSlotUI.FULLSTACK_COUNT) {
                stackableHotbarSlotIndex = i;
                break; // Prioritize stacking into an existing stack
            }

            // Remember the first empty hotbar slot
            if (!hotbarItemSlotsUI[i].HasItem && emptyHotbarSlotIndex == -1) {
                emptyHotbarSlotIndex = i;
            }

            i++;
        }

        // 2. Check the inventory for stackable slots
        i = 0; // Reset the index for inventory check
        while (i < inventoryItemSlotsUI.Length) {
            // Check if this slot has an item before accessing it
            if (inventoryItemSlotsUI[i].HasItem()) {
                // Check if this slot has the same item and can be stacked
                if (inventoryItemSlotsUI[i].GetItem().GetItemName() == (pickedUpObj as LLement).ElementName &&
                    inventoryItemSlotsUI[i].GetItem().GetStackCount() < HotbarSlotUI.FULLSTACK_COUNT) {
                    stackableInventorySlotIndex = i;
                    break; // Prioritize stacking into an existing stack
                }
            }

            // Remember the first empty inventory slot
            if (!inventoryItemSlotsUI[i].HasItem() && emptyInventorySlotIndex == -1) {
                emptyInventorySlotIndex = i;
            }

            i++;
        }

        // Decide the slot to use for stacking
        if (stackableHotbarSlotIndex != -1) {
            // Stack into the existing hotbar stack
            i = stackableHotbarSlotIndex;
            stackingIntoStack = true;
            puttingInInventory = false;
        }
        else if (stackableInventorySlotIndex != -1) {
            // Stack into the existing inventory stack
            i = stackableInventorySlotIndex;
            stackingIntoStack = true;
            puttingInInventory = true;
        }
        // Decide the slot to use for placing in an empty slot
        else if (emptyHotbarSlotIndex != -1) {
            // Use the first empty hotbar slot
            i = emptyHotbarSlotIndex;
            stackingIntoStack = false;
            puttingInInventory = false;
        }
        else if (emptyInventorySlotIndex != -1) {
            // Use the first empty inventory slot
            i = emptyInventorySlotIndex;
            stackingIntoStack = false;
            puttingInInventory = true;
        }
        else {
            // No empty slots or available stacks, return (inventory is full)
            Debug.Log("Inventory full");
            return;
        }



        if (!puttingInInventory) {
            // not putting in inventory
            if (!stackingIntoStack) {
                string itemName = ((LLement)pickedUpObj).ElementName;
                Sprite itemSprite = ((LLement)pickedUpObj).GetEmojiRenderer().sprite;
                InventoryItemUI newInventoryItem = Instantiate(inventoryItemUIPrefab, hotbarInventoryItemSlotsUI[i].transform.position, Quaternion.identity, hotbarInventoryItemSlotsUI[i].transform).GetComponent<InventoryItemUI>();
                newInventoryItem.InitItemInfo(itemSprite, itemName, pickedUpObj);

                hotbarItemSlotsUI[i].SetItemInfo(itemName, itemSprite, 1);
                hotbarPhysicalItems[i] = pickedUpObj;
                pickedUpObj.Pickup(player);

                Debug.Log("picked up item not into stack and into hotbar");
            }
            else {
                hotbarItemSlotsUI[i].AddItemToStack();
                hotbarInventoryItemSlotsUI[i].GetItem().AddItemToStack();
                Destroy(pickedUpObj.gameObject);

                Debug.Log("picked up item into stack and into hotbar");
            }

            if (pickedUpObj is LLement) {
                ((LLement)pickedUpObj).UIPanelSetActive(false);
            }
        } else {
            // putting into inventory
            if(!stackingIntoStack) {
                string itemName = ((LLement)pickedUpObj).ElementName;
                Sprite itemSprite = ((LLement)pickedUpObj).GetEmojiRenderer().sprite;

                InventoryItemUI newInventoryItem = Instantiate(inventoryItemUIPrefab, inventoryItemSlotsUI[i].transform.position, Quaternion.identity, inventoryItemSlotsUI[i].transform).GetComponent<InventoryItemUI>();
                newInventoryItem.InitItemInfo(itemSprite, itemName, pickedUpObj);
                pickedUpObj.Pickup(player);

                Debug.Log("picked up item not into stack and into inventory");
            } else {
                inventoryItemSlotsUI[i].GetItem().AddItemToStack();
                Destroy(pickedUpObj.gameObject);

                Debug.Log("picked up item into stack and into inventory");
            }
        }

        UpdatePhysicalItemAppearences();
    }
    public void DropItem() {
        if (hotbarItemSlotsUI[selectedIndex].StackCount == 1) {
            hotbarPhysicalItems[selectedIndex].Drop(player);
            
            hotbarItemSlotsUI[selectedIndex].RemoveItemFromStack();
            hotbarInventoryItemSlotsUI[selectedIndex].DestroyItem();

            if (hotbarPhysicalItems[selectedIndex] is LLement) {
                ((LLement)hotbarPhysicalItems[selectedIndex]).UIPanelSetActive(true);
            }
            hotbarPhysicalItems[selectedIndex].SetPickupTimer(4f);
            hotbarPhysicalItems[selectedIndex] = null;

            Debug.Log("dropping not from stack");

        } else if (hotbarItemSlotsUI[selectedIndex].StackCount > 1) {
            PickupableObject dupedItem = Instantiate(hotbarPhysicalItems[selectedIndex].gameObject, hotbarPhysicalItems[selectedIndex].gameObject.transform.position, hotbarPhysicalItems[selectedIndex].gameObject.transform.rotation, hotbarPhysicalItems[selectedIndex].gameObject.transform.parent).GetComponent<PickupableObject>();

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
            hotbarPhysicalItems[selectedIndex].Drop(player);

            hotbarItemSlotsUI[selectedIndex].RemoveItemFromStack();
            hotbarInventoryItemSlotsUI[selectedIndex].DestroyItem();

            if (hotbarPhysicalItems[selectedIndex] is LLement) {
                ((LLement)hotbarPhysicalItems[selectedIndex]).UIPanelSetActive(true);
            }
            hotbarPhysicalItems[selectedIndex].SetPickupTimer(4f);
            hotbarPhysicalItems[selectedIndex].GetComponent<Rigidbody>().AddForce(throwDirection * throwForce);
            hotbarPhysicalItems[selectedIndex] = null;

            Debug.Log("throwing not from stack");

        }
        else if (hotbarItemSlotsUI[selectedIndex].StackCount > 1) {
            PickupableObject dupedItem = Instantiate(hotbarPhysicalItems[selectedIndex].gameObject, hotbarPhysicalItems[selectedIndex].gameObject.transform.position, hotbarPhysicalItems[selectedIndex].gameObject.transform.rotation, hotbarPhysicalItems[selectedIndex].gameObject.transform.parent).GetComponent<PickupableObject>();

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
    private async void CombineButtonClickedHandler() {
        if(!combiningSlot1.HasItem() || !combiningSlot2.HasItem()) {
            return;
        }

        string name1 = combiningSlot1.GetItem().GetItemName();
        string name2 = combiningSlot2.GetItem().GetItemName();

        Destroy(combiningSlot1.GetItem().GetPhysicalItem().gameObject);
        Destroy(combiningSlot2.GetItem().GetPhysicalItem().gameObject);
        combiningSlot1.DestroyItem();
        combiningSlot2.DestroyItem();

        combineButton.GetComponentInChildren<Text>().text = "...";
        combineButton.interactable = false;

        string newItemName = await ChatGPTClient.Instance.SendChatRequest(
            "What object or concept comes to mind when I combine " + name1 + " with " + name2 +
            "? Say ONLY one simple word that represents an object or concept. Keep it simple, creative and engaging for a game where players combine elements. Do not make up words."
        );
        newItemName = newItemName.Replace(".", "").Trim().ToLower();

        ObjectMetadata metadata = await ObjectMetadataAPI.Instance.GetObjectMetadata(newItemName);
        Sprite newItemSprite = await EmojiConverter.GetEmojiSprite(metadata.emoji);

        LLement newElement = Instantiate(GameManager.Instance.GetLLementPrefab(), Vector3.zero, Quaternion.identity).GetComponent<LLement>();
        newElement.SetElementNameAndSprite(newItemName, newItemSprite);
        newElement.Pickup(player);

        InventoryItemUI resultItemUI = Instantiate(inventoryItemUIPrefab, resultSlot.transform.position, Quaternion.identity, resultSlot.transform).GetComponent<InventoryItemUI>();
        resultItemUI.InitItemInfo(newItemSprite, newItemName, newElement);

        combineButton.GetComponentInChildren<Text>().text = "Combine";
        combineButton.interactable = true;

        Debug.Log("combining " + name1 + " and " + name2 + " to make " + newItemName);
    }
    public void ClearHotbarSlot(int index) {
        Debug.Log("hotbar slot cleared");
        hotbarPhysicalItems[index] = null;
        hotbarItemSlotsUI[index].SetItemInfo("", null, 0);

        UpdatePhysicalItemAppearences();
    }
    public void SetHotbarSlot(int index, PickupableObject physicalObj, string itemName, Sprite itemSprite, int itemCount) {
        Debug.Log("hotbar slot updated");
        hotbarPhysicalItems[index] = physicalObj;
        hotbarItemSlotsUI[index].SetItemInfo(itemName, itemSprite, itemCount);

        UpdatePhysicalItemAppearences();
    }
    private void OnOpenCloseInventoryHandler(InputAction.CallbackContext context) {
        if(!IsHoldingAnItemInInv() && !isCombining) {
            isInventoryOpen = !isInventoryOpen;
            inventoryUI.SetActive(isInventoryOpen);
        }
    }

    private void UpdatePhysicalItemAppearences() {
        // set not active for hotbar items except the one held
        foreach(PickupableObject item in hotbarPhysicalItems) { 
            if(item == null) continue;

            item.gameObject.SetActive(false);
        }
        if(hotbarPhysicalItems[selectedIndex] != null) {
            hotbarPhysicalItems[selectedIndex].gameObject.SetActive(true);
        }
        
        // set not active for all 
        foreach(InventorySlotUI invSlot in inventoryItemSlotsUI) {
            if(invSlot.HasItem()) {
                invSlot.GetItem().GetPhysicalItem().gameObject.SetActive(false);
            }
        }
    }
    private bool IsHoldingAnItemInInv() {
        foreach(InventoryItemUI invItem in FindObjectsOfType<InventoryItemUI>()) {
            if(invItem.IsPickedUp()) return true;
        }
        return false;
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

        UpdatePhysicalItemAppearences();
    }
    public bool IsSlotResultSlot(InventorySlotUI slot) {
        return resultSlot == slot;
    }
    public bool IsInventoryOpen() { return isInventoryOpen; }
    public PickupableObject GetCurrentHoldingItem() { return hotbarPhysicalItems[selectedIndex]; }
}
