using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour
{
    public static readonly int FULLSTACK_COUNT = 64;

    private PickupableObject pickupableObject;
    private Image itemImage;
    private Sprite itemSprite;
    private string itemName;
    private int itemCount;
    [SerializeField] private TextMeshProUGUI itemCountTxt;
    //[SerializeField] private TextMeshProUGUI itemNameTxt;

    private void Awake() {
        itemImage = GetComponent<Image>();
    }
    private void Start() {
        itemImage.sprite = itemSprite;
    }

    public void InitItemInfo(Sprite itemSprite, string itemName, PickupableObject pickupableObject) {
        itemCount = 1;
        this.itemSprite = itemSprite;
        this.itemName = itemName;
        this.pickupableObject = pickupableObject;

        UpdateItemInfo();
    }
    public void UpdateItemInfo() {
        itemCountTxt.text = itemCount.ToString();
    }
    public void AddItemToStack() {
        itemCount++;
        UpdateItemInfo();
    }
    public void RemoveItemFromStack() {
        itemCount--;
        UpdateItemInfo();
    }
}
